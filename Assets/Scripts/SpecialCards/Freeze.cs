using UnityEngine;

public sealed class Freeze
{
    private const string CardName = "Freeze";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellFreeze, CardName);
    }

    public CardEffectResult Apply(CardRuntimeState sourceCard, CardTarget target)
    {
        if (!(sourceCard?.SourceCard is SpellCardData spellCard))
        {
            return CardEffectResult.Failure("NO_FREEZE_SPELL", "Freeze needs a spell card source.");
        }

        if (target.targetCard == null)
        {
            return CardEffectResult.Failure("NO_TARGET_CARD", "Freeze needs a target unit card.");
        }

        int durationTurns = Mathf.Max(0, spellCard.effectDurationTurns);
        if (durationTurns <= 0)
        {
            return CardEffectResult.Failure("INVALID_DURATION", "Freeze needs effectDurationTurns above zero.");
        }

        Unit targetUnit = FindUnitForCard(target.targetCard);
        if (targetUnit == null)
        {
            return CardEffectResult.Failure("NO_TARGET_UNIT", "Freeze could not resolve the targeted board unit.");
        }

        targetUnit.ApplyFreeze(durationTurns);
        return CardEffectResult.Success($"Target frozen for {durationTurns} turn(s).");
    }

    private static Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
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
}
