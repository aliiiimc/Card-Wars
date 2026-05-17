using UnityEngine;

public sealed class HealCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.heal";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    //Ali:
    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (sourceCard != null && sourceCard.SourceCard is SpellCardData)
        {
            SpellManager spellManager = SpellManager.GetOrCreate();
            return spellManager.ApplyHealSpell(context, sourceCard, target, amount);
        }

        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        // Si la cible est un Fort allié, on soigne le Fort
        // Sinon, on soigne une unité avec targetCard
        int safeAmount = Mathf.Max(0, amount);

        if (target.type == CardTargetType.AllyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardEffectResult.Failure("NO_TARGET_PLAYER", "Heal effect needs a fort owner id.");
            }

            context.Writer.ApplyFortHeal(target.targetPlayerId, safeAmount);
            return CardEffectResult.Success("Fort heal applied.", healApplied: safeAmount);
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Heal", out failure))// out failure :Appelle la méthode, 
        // Si elle échoue, elle met une valeur dans failure
        // Ensuite on retourne cette valeur
        {
            return failure;
        }

        context.Writer.ApplyHeal(target.targetCard, safeAmount);

        return CardEffectResult.Success("Heal applied.", healApplied: safeAmount);

    }
}
