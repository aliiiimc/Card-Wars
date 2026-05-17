using UnityEngine;

public sealed class IncomeBoostCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.income_boost";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (sourceCard != null && sourceCard.SourceCard is SpellCardData)
        {
            SpellManager spellManager = SpellManager.GetOrCreate();
            return spellManager.ApplyBoostSpell(context, sourceCard, amount);
        }

        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
        {
            return CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing.");
        }

        int safeAmount = Mathf.Max(0, amount);
        context.Writer.AddRevenue(context.ActingPlayerKey, safeAmount);

        return CardEffectResult.Success("Income boost applied.", revenueGained: safeAmount);
    }
}
