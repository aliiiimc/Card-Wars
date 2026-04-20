// Interface for target validation strategy implementations that determine whether a specific card can target a given entity or tile.
public interface ICardTargetValidator
{
    string ValidatorId { get; }

    CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target);
}
