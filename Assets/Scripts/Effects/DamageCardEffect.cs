using UnityEngine;

public sealed class DamageCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.damage";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Damage", out failure))
        {
            return failure;
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.ApplyDamage(target.targetCard, safeAmount);

        return CardEffectResult.Success("Damage applied.", damageDealt: safeAmount);
    }
}
