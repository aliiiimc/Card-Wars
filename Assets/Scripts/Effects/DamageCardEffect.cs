using UnityEngine;

public sealed class DamageCardEffect : MonoBehaviour, ICardEffect
{
    [SerializeField] private string effectId = "effect.damage";
    [SerializeField] private int amount = 1;

    public string EffectId => effectId;

    //Ali : 
    public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (sourceCard != null && sourceCard.SourceCard is SpellCardData)
        {
            SpellManager spellManager = SpellManager.GetOrCreate();
            return spellManager.ApplyDamageSpell(context, sourceCard, target, amount);
        }

        if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
        {
            return failure;
        }

        // Avant: Damage exigeait target.targetCard
        // Donc si la cible était un Fort, ça échouait
        // Maintenant: si la cible est EnemyFort, on passe par ApplyFortDamage(...)

        int safeAmount = Mathf.Max(0, amount);

        if (target.type == CardTargetType.EnemyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardEffectResult.Failure("NO_TARGET_PLAYER", "Damage effect needs a fort owner id.");
            }

            context.Writer.ApplyFortDamage(target.targetPlayerId, safeAmount);
            return CardEffectResult.Success("Fort damage applied.", damageDealt: safeAmount);
        }

        if (!CardEffectGuards.TryRequireTargetCard(target, "Damage", out failure))
        {
            return failure;
        }

        context.Writer.ApplyDamage(target.targetCard, safeAmount);

        return CardEffectResult.Success("Damage applied.", damageDealt: safeAmount);
    }
}
