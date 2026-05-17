using System.Collections.Generic;
using UnityEngine;

public sealed class SpellManager : MonoBehaviour
{
    private readonly Dictionary<CardRuntimeState, Spell> spellsBySource = new Dictionary<CardRuntimeState, Spell>();
    private readonly List<Spell> activePersistentSpells = new List<Spell>();
    private FortGame.UI.HUDManager hudManager;

    public static SpellManager GetOrCreate()
    {
        SpellManager manager = FindFirstObjectByType<SpellManager>();
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("SpellManager");
        return managerObject.AddComponent<SpellManager>();
    }

    public Spell FindSpell(CardRuntimeState sourceCard)
    {
        if (sourceCard == null)
        {
            return null;
        }

        spellsBySource.TryGetValue(sourceCard, out Spell spell);
        return spell;
    }

    public bool Remove(CardRuntimeState sourceCard)
    {
        if (sourceCard == null)
        {
            return false;
        }

        if (!spellsBySource.TryGetValue(sourceCard, out Spell spell))
        {
            return false;
        }

        spellsBySource.Remove(sourceCard);
        activePersistentSpells.Remove(spell);
        if (spell != null)
        {
            spell.Remove();
        }

        return true;
    }

    public void ConsumePersistentDurations(string ownerKey = null)
    {
        for (int i = activePersistentSpells.Count - 1; i >= 0; i--)
        {
            Spell spell = activePersistentSpells[i];
            if (spell == null)
            {
                activePersistentSpells.RemoveAt(i);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(ownerKey) && spell.owner != ownerKey)
            {
                continue;
            }

            bool expired = spell.ConsumeDurationTurn();
            if (!expired)
            {
                continue;
            }

            activePersistentSpells.RemoveAt(i);
            if (spell.sourceCard != null)
            {
                spellsBySource.Remove(spell.sourceCard);
            }

            NotifySpellExpired(spell);
            spell.Remove();
        }
    }

    public CardEffectResult ApplyDamageSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (target.type == CardTargetType.EnemyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CompleteSpell(spell, CardEffectResult.Failure("NO_TARGET_PLAYER", "Damage spell needs a fort owner id."));
            }

            context.Writer.ApplyFortDamage(target.targetPlayerId, safeAmount);
            return CompleteSpell(spell, CardEffectResult.Success("Fort damage applied.", damageDealt: safeAmount));
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Damage spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        context.Writer.ApplyDamage(target.targetCard, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Damage applied.", damageDealt: safeAmount));
    }

    public CardEffectResult ApplyHealSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (target.type == CardTargetType.AllyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CompleteSpell(spell, CardEffectResult.Failure("NO_TARGET_PLAYER", "Heal spell needs a fort owner id."));
            }

            context.Writer.ApplyFortHeal(target.targetPlayerId, safeAmount);
            return CompleteSpell(spell, CardEffectResult.Success("Fort heal applied.", healApplied: safeAmount));
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Heal spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        context.Writer.ApplyHeal(target.targetCard, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Heal applied.", healApplied: safeAmount));
    }

    public CardEffectResult ApplyBuffSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int healAmount, int damageBoostAmount, int speedBoostAmount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Buff spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        SpeedSpellCardData speedSpellCard = sourceCard != null ? sourceCard.SourceCard as SpeedSpellCardData : null;
        int movementCapacityMultiplier = speedSpellCard != null
            ? Mathf.Max(1, speedSpellCard.movementCapacityMultiplier)
            : 1;

        if (movementCapacityMultiplier > 1)
        {
            int durationTurns = speedSpellCard != null ? speedSpellCard.effectDurationTurns : 0;
            if (durationTurns <= 0)
            {
                return CompleteSpell(
                    spell,
                    CardEffectResult.Failure(
                        "INVALID_DURATION",
                        "Movement multiplier spells need effectDurationTurns above zero."));
            }

            Unit targetUnit = FindUnitForCard(target.targetCard);
            if (targetUnit == null)
            {
                return CompleteSpell(
                    spell,
                    CardEffectResult.Failure(
                        "NO_TARGET_UNIT",
                        "Buff spell could not resolve the targeted board unit."));
            }

            targetUnit.ApplyMovementRangeMultiplier(movementCapacityMultiplier, durationTurns);
            return CompleteSpell(
                spell,
                CardEffectResult.Success(
                    $"Movement capacity x{movementCapacityMultiplier} applied for {durationTurns} turn(s)."));
        }

        int safeHeal = Mathf.Max(0, healAmount);
        int safeDamageBoost = Mathf.Max(0, damageBoostAmount);
        int safeSpeedBoost = Mathf.Max(0, speedBoostAmount);
        bool didSomething = false;

        if (safeHeal > 0)
        {
            context.Writer.ApplyHeal(target.targetCard, safeHeal);
            didSomething = true;
        }

        if (safeDamageBoost > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, safeDamageBoost);
            didSomething = true;
        }

        if (safeSpeedBoost > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, safeSpeedBoost);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_BUFF_VALUES", "Set at least one buff value above zero."));
        }

        return CompleteSpell(spell, CardEffectResult.Success("Buff applied.", healApplied: safeHeal));
    }

    public CardEffectResult ApplyDebuffSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int damageAmount, int damageReductionAmount, int speedReductionAmount)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Debuff spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        int safeDamage = Mathf.Max(0, damageAmount);
        int safeDamageReduction = Mathf.Max(0, damageReductionAmount);
        int safeSpeedReduction = Mathf.Max(0, speedReductionAmount);
        bool didSomething = false;

        if (safeDamage > 0)
        {
            context.Writer.ApplyDamage(target.targetCard, safeDamage);
            didSomething = true;
        }

        if (safeDamageReduction > 0)
        {
            context.Writer.ModifyDamage(target.targetCard, -safeDamageReduction);
            didSomething = true;
        }

        if (safeSpeedReduction > 0)
        {
            context.Writer.ModifyMovement(target.targetCard, -safeSpeedReduction);
            didSomething = true;
        }

        if (!didSomething)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_DEBUFF_VALUES", "Set at least one debuff value above zero."));
        }

        return CompleteSpell(spell, CardEffectResult.Success("Debuff applied.", damageDealt: safeDamage));
    }

    public CardEffectResult ApplyUtilitySpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int movementDelta)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Utility spell", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        int safeDelta = Mathf.Max(0, movementDelta);
        if (safeDelta <= 0)
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_UTILITY_VALUE", "Set movement delta above zero."));
        }

        context.Writer.ModifyMovement(target.targetCard, safeDelta);
        return CompleteSpell(spell, CardEffectResult.Success("Utility effect applied."));
    }

    public CardEffectResult ApplyBoostSpell(CardEffectContext context, CardRuntimeState sourceCard, int amount)
    {
        if (!TryPrepareSpell(context, sourceCard, default, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return CompleteSpell(spell, CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing."));
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.AddRevenue(context.ActingPlayerKey, safeAmount);
        return CompleteSpell(spell, CardEffectResult.Success("Income boost applied.", revenueGained: safeAmount));
    }

    public CardEffectResult ApplySummonSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, bool requireTileToBeEmpty)
    {
        if (!TryPrepareSpell(context, sourceCard, target, out Spell spell, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetType(target, CardTargetType.Tile, "Summon spell needs a tile target.", out failure))
        {
            return CompleteSpell(spell, failure);
        }

        if (context.Board != null)
        {
            if (!CardEffectGuards.TryRequireBoardAndValidTile(context, target.tile, "Target tile is invalid.", out failure))
            {
                return CompleteSpell(spell, failure);
            }

            if (requireTileToBeEmpty && !CardEffectGuards.TryRequireTileEmpty(context, target.tile, "Target tile is occupied.", out failure))
            {
                return CompleteSpell(spell, failure);
            }
        }

        context.Writer.ManifestCard(sourceCard, target.tile);
        return CompleteSpell(spell, CardEffectResult.Success("Summon applied."));
    }

    private bool TryPrepareSpell(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, out Spell spell, out CardEffectResult failure)
    {
        spell = null;

        if (!CardEffectGuards.TryRequireContextAndWriter(context, out failure))
        {
            return false;
        }

        if (!CardEffectGuards.TryRequireSourceCard(sourceCard, out failure))
        {
            return false;
        }

        if (!(sourceCard.SourceCard is SpellCardData))
        {
            failure = CardEffectResult.Failure("NO_SPELL_CARD", "Spell manager can only resolve spell cards.");
            return false;
        }

        spell = CreateOrUpdateSpell(sourceCard, ResolveSpellOwner(context, target), target);
        failure = default;
        return true;
    }

    private Spell CreateOrUpdateSpell(CardRuntimeState sourceCard, string owner, CardTarget target)
    {
        if (spellsBySource.TryGetValue(sourceCard, out Spell existing) && existing != null)
        {
            existing.Initialize(owner, sourceCard, target);
            return existing;
        }

        GameObject spellObject = new GameObject($"Spell_{sourceCard.SourceCard.DisplayName}");
        spellObject.transform.SetParent(transform, true);

        Spell spell = spellObject.AddComponent<Spell>();
        spell.Initialize(owner, sourceCard, target);

        spellsBySource[sourceCard] = spell;
        return spell;
    }

    private CardEffectResult CompleteSpell(Spell spell, CardEffectResult result)
    {
        if (spell == null)
        {
            return result;
        }

        if (!result.Succeeded)
        {
            if (spell.sourceCard != null)
            {
                Remove(spell.sourceCard);
            }
            else
            {
                spell.Remove();
            }

            return result;
        }

        spell.MarkResolved();
        NotifySpellResolved(spell);

        if (spell.remainingDurationTurns > 0)
        {
            if (!activePersistentSpells.Contains(spell))
            {
                activePersistentSpells.Add(spell);
            }

            return result;
        }

        if (spell.sourceCard != null)
        {
            Remove(spell.sourceCard);
        }
        else
        {
            spell.Remove();
        }

        return result;
    }

    private static string ResolveSpellOwner(CardEffectContext context, CardTarget target)
    {
        if (context != null && !string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return context.ActingPlayerKey;
        }

        if (!string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return target.targetPlayerId;
        }

        return "none";
    }

    private static Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && ReferenceEquals(unit.RuntimeCard, card))
            {
                return unit;
            }
        }

        return null;
    }

    private void NotifySpellResolved(Spell spell)
    {
        if (spell == null || spell.sourceCard == null || spell.sourceCard.SourceCard == null)
        {
            return;
        }

        string cardName = spell.sourceCard.SourceCard.DisplayName;
        string ownerLabel = GetOwnerLabel(spell.owner);

        if (spell.remainingDurationTurns > 0)
        {
            string message = $"{ownerLabel} cast {cardName}. Effect active for {spell.remainingDurationTurns} turn(s).";
            BroadcastNotification(message);
            return;
        }

        BroadcastNotification($"{ownerLabel} cast {cardName}. Effect resolved.");
    }

    private void NotifySpellExpired(Spell spell)
    {
        if (spell == null || spell.sourceCard == null || spell.sourceCard.SourceCard == null)
        {
            return;
        }

        string cardName = spell.sourceCard.SourceCard.DisplayName;
        string ownerLabel = GetOwnerLabel(spell.owner);
        BroadcastNotification($"{cardName} effect from {ownerLabel} has ended.");
    }

    private void BroadcastNotification(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<FortGame.UI.HUDManager>();
        }

        if (hudManager != null)
        {
            hudManager.ShowInfo(message);
        }

        Debug.Log($"[SpellManager] {message}");
    }

    private static string GetOwnerLabel(string owner)
    {
        if (owner == PlayerKeyResolver.PlayerOneKey)
        {
            return "Player";
        }

        if (owner == PlayerKeyResolver.PlayerTwoKey)
        {
            return "Enemy";
        }

        return string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner;
    }
}
