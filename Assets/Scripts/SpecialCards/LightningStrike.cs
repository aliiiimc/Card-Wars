using UnityEngine;

public sealed class LightningStrike
{
    private const string CardName = "Lightning Strike";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellLightningStrike, CardName);
    }

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target, int amount)
    {
        if (context == null || context.Writer == null)
        {
            return CardEffectResult.Failure("NO_CONTEXT", "Lightning Strike needs an effect context and writer.");
        }

        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return CardEffectResult.Failure("NO_LIGHTNING_SPELL", "Lightning Strike needs a spell card source.");
        }

        int safeAmount = Mathf.Max(0, amount);
        if (target.type == CardTargetType.EnemyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardEffectResult.Failure("NO_TARGET_PLAYER", "Lightning Strike needs a fort owner id.");
            }

            context.Writer.ApplyFortDamage(target.targetPlayerId, safeAmount);
            return CardEffectResult.Success("Fort damage applied.", damageDealt: safeAmount);
        }

        if (target.targetCard == null)
        {
            return CardEffectResult.Failure("NO_TARGET_CARD", "Lightning Strike needs a target card.");
        }

        context.Writer.ApplyDamage(target.targetCard, safeAmount);
        return CardEffectResult.Success("Damage applied.", damageDealt: safeAmount);
    }
}
