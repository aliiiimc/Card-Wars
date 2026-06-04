using UnityEngine;

public static class CardEffectFactory
{
    public static ICardEffect Create(CardData cardData)
    {
        if (cardData is CharacterCardData || cardData is WorldEffectCardData)
        {
            return new ManifestEffect();
        }

        if (cardData is SpellCardData spellCard)
        {
            switch (spellCard.effectType)
            {
                case SpellEffectType.Damage:
                    return new SpellDamageEffect(spellCard);
                case SpellEffectType.Heal:
                    return new SpellHealEffect(spellCard);
                case SpellEffectType.Buff:
                    return new SpellBuffEffect(spellCard);
                case SpellEffectType.Debuff:
                    return new SpellDebuffEffect(spellCard);
                case SpellEffectType.Boost:
                    return new IncomeEffect(spellCard);
                case SpellEffectType.Summon:
                    return new SummonSpellEffect(spellCard);
                case SpellEffectType.Utility:
                    return new UtilityTempoEffect(spellCard);
            }
        }

        return new UnsupportedEffect();
    }

    private sealed class ManifestEffect : ICardEffect
    {
        public string EffectId => "effect.factory.manifest";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
            {
                return failure;
            }

            if (!CardEffectGuards.TryRequireSourceCard(sourceCard, out failure))
            {
                return failure;
            }

            if (!CardEffectGuards.TryRequireTargetType(target, CardTargetType.Tile, "Card requires a tile target.", out failure))
            {
                return failure;
            }

            context.Writer.ManifestCard(sourceCard, target.tile);
            return CardEffectResult.Success("Card manifested.");
        }
    }

    private abstract class SpellManagerEffectBase : ICardEffect
    {
        protected readonly int amount;

        protected SpellManagerEffectBase(SpellCardData spellCard)
        {
            amount = spellCard != null ? Mathf.Max(0, spellCard.effectPower) : 0;
        }

        public abstract string EffectId { get; }
        public abstract CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target);
    }

    private sealed class SpellDamageEffect : SpellManagerEffectBase
    {
        public SpellDamageEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.damage";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplyDamageSpell(context, sourceCard, target, amount);
        }
    }

    private sealed class SpellHealEffect : SpellManagerEffectBase
    {
        public SpellHealEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.heal";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplyHealSpell(context, sourceCard, target, amount);
        }
    }

    private sealed class SpellBuffEffect : SpellManagerEffectBase
    {
        public SpellBuffEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.buff";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplyBuffSpell(
                context,
                sourceCard,
                target,
                healAmount: 0,
                damageBoostAmount: amount,
                speedBoostAmount: amount);
        }
    }

    private sealed class SpellDebuffEffect : SpellManagerEffectBase
    {
        public SpellDebuffEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.debuff";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplyDebuffSpell(
                context,
                sourceCard,
                target,
                damageAmount: 0,
                damageReductionAmount: 0,
                speedReductionAmount: amount);
        }
    }

    private sealed class UtilityTempoEffect : SpellManagerEffectBase
    {
        public UtilityTempoEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.utility";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplyUtilitySpell(context, sourceCard, target, amount);
        }
    }

    private sealed class IncomeEffect : ICardEffect
    {
        private readonly int amount;

        public IncomeEffect(SpellCardData spellCard)
        {
            amount = spellCard != null ? Mathf.Max(0, spellCard.effectPower) : 0;
        }

        public string EffectId => "effect.factory.income";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (!CardEffectGuards.TryRequireContextAndWriter(context, out CardEffectResult failure))
            {
                return failure;
            }

            return SpellManager.GetOrCreate().ApplyBoostSpell(context, sourceCard, amount);
        }
    }

    private sealed class SummonSpellEffect : SpellManagerEffectBase
    {
        public SummonSpellEffect(SpellCardData spellCard)
            : base(spellCard)
        {
        }

        public override string EffectId => "effect.factory.summon";

        public override CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return SpellManager.GetOrCreate().ApplySummonSpell(context, sourceCard, target, requireTileToBeEmpty: true);
        }
    }

    private sealed class UnsupportedEffect : ICardEffect
    {
        public string EffectId => "effect.factory.unsupported";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return CardEffectResult.Failure("NO_EFFECT_MAPPING", "No effect mapping found for this card.");
        }
    }
}
