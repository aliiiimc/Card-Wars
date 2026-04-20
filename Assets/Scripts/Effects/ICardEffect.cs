// Interface for concrete card effect implementations (damage, healing, summoning, etc.) that execute card ability logic.
public interface ICardEffect
{
    string EffectId { get; }

    CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target);
}
