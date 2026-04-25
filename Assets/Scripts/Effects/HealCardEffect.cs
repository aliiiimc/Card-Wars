using UnityEngine;

public sealed class HealCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.heal";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Heal", out failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.ApplyHeal(target.targetCard, safeAmount);

        return CardEffectResult.Success("Heal applied.", healApplied: safeAmount);
    }
}
