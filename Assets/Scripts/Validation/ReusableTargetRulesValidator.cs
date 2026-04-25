using System;
using UnityEngine;

public sealed class ReusableTargetRulesValidator : MonoBehaviour, ICardTargetValidator
{
    [SerializeField] private string validatorId = "target.rules.reusable";
    [SerializeField] private bool requireFreeTile = true;

    public string ValidatorId => validatorId;

    public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (context == null)
        {
            return CardValidationResult.Invalid("NO_CONTEXT", "Validation context is missing.");
        }

        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            return CardValidationResult.Invalid("NO_CARD", "Source card is missing.");
        }

        switch (target.type)
        {
            case CardTargetType.AllyUnit:
                return ValidateUnitTarget(context, target, shouldBeAlly: true);

            case CardTargetType.EnemyUnit:
                return ValidateUnitTarget(context, target, shouldBeAlly: false);

            case CardTargetType.Tile:
                return ValidateTileTarget(context, target);

            case CardTargetType.AllyFort:
                return ValidateFortTarget(context, target, shouldBeAlly: true);

            case CardTargetType.EnemyFort:
                return ValidateFortTarget(context, target, shouldBeAlly: false);

            default:
                return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported.");
        }
    }

    private CardValidationResult ValidateTileTarget(CardValidationContext context, CardTarget target)
    {
        if (context.Board == null)
        {
            return CardValidationResult.Invalid("NO_BOARD", "Board state reader is missing.");
        }

        if (!context.Board.IsTileValid(target.tile))
        {
            return CardValidationResult.Invalid("INVALID_TILE", "Tile is outside board bounds.");
        }

        if (requireFreeTile && context.Board.IsTileOccupied(target.tile))
        {
            return CardValidationResult.Invalid("TILE_OCCUPIED", "Tile is occupied.");
        }

        return CardValidationResult.Valid();
    }

    private static CardValidationResult ValidateUnitTarget(CardValidationContext context, CardTarget target, bool shouldBeAlly)
    {
        if (target.targetCard == null)
        {
            return CardValidationResult.Invalid("NO_TARGET_CARD", "Unit target requires target card.");
        }

        if (!(target.targetCard.SourceCard is CharacterCardData))
        {
            return CardValidationResult.Invalid("NOT_UNIT", "Target card is not a unit.");
        }

        if (!target.targetCard.IsManifestedOnBoard)
        {
            return CardValidationResult.Invalid("NOT_ON_BOARD", "Target unit is not on the board.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Unit target player id is required.");
        }

        string expected = shouldBeAlly ? context.ActingPlayerKey : context.OpponentPlayerKey;
        if (target.targetPlayerId != expected)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", shouldBeAlly
                ? "Target unit is not allied."
                : "Target unit is not an enemy.");
        }

        return CardValidationResult.Valid();
    }

    private CardValidationResult ValidateFortTarget(CardValidationContext context, CardTarget target, bool shouldBeAlly)
    {
        if (context.Board == null)
        {
            return CardValidationResult.Invalid("NO_BOARD", "Board state reader is missing.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Fort target player id is required.");
        }

        string expectedPlayer = shouldBeAlly ? context.ActingPlayerKey : context.OpponentPlayerKey;
        if (target.targetPlayerId != expectedPlayer)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", shouldBeAlly
                ? "Target fort is not allied."
                : "Target fort is not the enemy fort.");
        }

        if (!context.Board.IsTileValid(target.tile))
        {
            return CardValidationResult.Invalid("INVALID_TILE", "Fort target tile is outside board bounds.");
        }

        if (!string.IsNullOrWhiteSpace(target.targetEntityId) && !string.Equals(target.targetEntityId, "fort", StringComparison.OrdinalIgnoreCase))
        {
            return CardValidationResult.Invalid("WRONG_TARGET_ENTITY", "Fort target entity id must be 'fort'.");
        }

        if (!context.Board.IsTileOccupied(target.tile))
        {
            return CardValidationResult.Invalid("FORT_NOT_PRESENT", "Fort tile is not occupied.");
        }

        return CardValidationResult.Valid();
    }

}
