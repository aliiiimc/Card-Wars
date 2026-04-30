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
                return ValidateTileTarget(context, sourceCard, target); //Ali: tile validation now depends on the source card type.

            case CardTargetType.AllyFort:
                return ValidateFortTarget(context, target, shouldBeAlly: true);

            case CardTargetType.EnemyFort:
                return ValidateFortTarget(context, target, shouldBeAlly: false);

            default:
                return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported.");
        }
    }

    //Ali: validate tile targets with card-specific rules for Character, World Effect, and Spell cards.
    private CardValidationResult ValidateTileTarget(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
    {
        if (context.Board == null)
        {
            return CardValidationResult.Invalid("NO_BOARD", "Board state reader is missing.");
        }


        if (!context.Board.IsTileValid(target.tile))
        {
            return CardValidationResult.Invalid("INVALID_TILE", "Tile is outside board bounds.");
        }

        //Ali: HexGrid is needed for deployment-zone and half-board rule checks.
        HexGrid grid = FindFirstObjectByType<HexGrid>();
        if (grid == null)
        {
            return CardValidationResult.Invalid("NO_GRID", "HexGrid is missing.");
        }

        //Ali: Character cards must target an empty tile inside the acting player's deployment zone.
        if (sourceCard.SourceCard is CharacterCardData)
        {
            if (context.Board.IsTileOccupied(target.tile))
            {
                return CardValidationResult.Invalid("TILE_OCCUPIED", "Character must be placed on an empty tile.");
            }

            if (!grid.IsInPlayerDeploymentZone(target.tile, context.ActingPlayerKey))
            {
                return CardValidationResult.Invalid("OUTSIDE_DEPLOYMENT_ZONE", "Character must be placed in the owner's 2-column deployment zone.");
            }

            return CardValidationResult.Valid();
        }

        //Ali: World Effect cards must target an empty tile inside the acting player's half of the board.
        if (sourceCard.SourceCard is WorldEffectCardData)
        {
            if (context.Board.IsTileOccupied(target.tile))
            {
                return CardValidationResult.Invalid("TILE_OCCUPIED", "World Effect must be placed on an empty tile.");
            }

            if (!grid.IsInPlayerHalf(target.tile, context.ActingPlayerKey))
            {
                return CardValidationResult.Invalid("OUTSIDE_OWNER_HALF", "World Effect must be placed in the owner's half of the board.");
            }

            return CardValidationResult.Valid();
        }

        //Ali: Spells do not target empty tiles by default in v1; they should target units or forts.
        if (sourceCard.SourceCard is SpellCardData)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Spell cards do not target empty tiles by default in v1.");
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

        //Ali: fort validation now checks the real board tile type and owner, not only generic occupancy.
        HexGrid grid = FindFirstObjectByType<HexGrid>();
        if (grid == null)
        {
            return CardValidationResult.Invalid("NO_GRID", "HexGrid is missing.");
        }

        HexTile fortTile = grid.GetTile(target.tile);
        if (fortTile == null || fortTile.tileType != "fort")
        {
            return CardValidationResult.Invalid("FORT_NOT_PRESENT", "Target tile does not contain a fort.");
        }

        if (fortTile.owner != target.targetPlayerId)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Fort owner does not match target player.");
        }

        return CardValidationResult.Valid();

    }

}
