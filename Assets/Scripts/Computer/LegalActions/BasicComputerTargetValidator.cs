using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Baseline validator used by the AI legal-action reader until full validator routing is available.
    /// </summary>
    public sealed class BasicComputerTargetValidator : ICardTargetValidator
    {
        public string ValidatorId => "computer.basic";

        public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null)
            {
                return CardValidationResult.Invalid("NO_CONTEXT", "Validation context is missing.");
            }

            if (sourceCard?.SourceCard == null)
            {
                return CardValidationResult.Invalid("NO_CARD", "Source card is missing.");
            }

            if (context.Board == null)
            {
                return CardValidationResult.Invalid("NO_BOARD", "Board reader is missing.");
            }

            switch (target.type)
            {
                case CardTargetType.Tile:
                    if (!context.Board.IsTileValid(target.tile))
                    {
                        return CardValidationResult.Invalid("INVALID_TILE", "Target tile does not exist.");
                    }

                    if (context.Board.IsTileOccupied(target.tile))
                    {
                        return CardValidationResult.Invalid("OCCUPIED_TILE", "Target tile is occupied.");
                    }

                    // Ali: spells do not target empty tiles in v1.
                    if (sourceCard.SourceCard is SpellCardData)
                    {
                        return CardValidationResult.Invalid(
                            "WRONG_TARGET_TYPE",
                            "Spell cards do not target empty tiles by default in v1.");
                    }

                    // Ali: Character cards must be placed in the owner's deployment zone.
                    if (sourceCard.SourceCard is CharacterCardData)
                    {
                        HexGrid grid = Object.FindFirstObjectByType<HexGrid>();

                        if (!BoardPlacementRules.CanPlaceCharacter(target.tile, context.ActingPlayerKey, grid))
                        {
                            return CardValidationResult.Invalid(
                                "OUTSIDE_DEPLOYMENT_ZONE",
                                "Character must be placed in deployment zone.");
                        }
                    }

                    // Ali: use the shared World Effect placement helper so AI follows the same placement rule as the player.
                    if (sourceCard.SourceCard is WorldEffectCardData)
                    {
                        HexGrid grid = Object.FindFirstObjectByType<HexGrid>();

                        if (!BoardPlacementRules.CanPlaceWorldEffect(target.tile, context.ActingPlayerKey, grid))
                        {
                            return CardValidationResult.Invalid(
                                "OUTSIDE_OWNER_HALF",
                                "World Effect must be placed in the owner's half.");
                        }
                    }

                    return CardValidationResult.Valid();

                case CardTargetType.EnemyFort:
                    // Ali: only damage spells can target the enemy Fort.
                    if (sourceCard.SourceCard is SpellCardData spellCard && spellCard.effectType != SpellEffectType.Damage)
                    {
                        return CardValidationResult.Invalid(
                            "WRONG_TARGET_TYPE",
                            "Only damage spells can target the enemy Fort.");
                    }

                    if (string.IsNullOrWhiteSpace(target.targetPlayerId))
                    {
                        return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Enemy fort target player is missing.");
                    }

                    if (target.targetPlayerId != context.OpponentPlayerKey)
                    {
                        return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target fort does not belong to opponent.");
                    }

                    // Ali: AI validation must check the real Fort on the real tile, like the reusable validator.
                    HexGrid fortGrid = Object.FindFirstObjectByType<HexGrid>();
                    if (fortGrid == null)
                    {
                        return CardValidationResult.Invalid("NO_GRID", "HexGrid is missing.");
                    }

                    HexTile fortTile = fortGrid.GetTile(target.tile);
                    if (fortTile == null || fortTile.tileType != "fort")
                    {
                        return CardValidationResult.Invalid("FORT_NOT_PRESENT", "Target tile does not contain a fort.");
                    }

                    if (fortTile.owner != target.targetPlayerId)
                    {
                        return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Fort owner does not match target player.");
                    }

                    return CardValidationResult.Valid();

                default:
                    return CardValidationResult.Invalid("UNSUPPORTED_TARGET", "This validator only supports tile and enemy fort targeting.");
            }
        }
    }
}
