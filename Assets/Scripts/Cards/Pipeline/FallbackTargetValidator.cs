public sealed class FallbackTargetValidator : ICardTargetValidator
{
    public string ValidatorId => "target.rules.fallback";

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

        if (target.type == CardTargetType.Tile)
        {
            if (context.Board != null)
            {
                if (!context.Board.IsTileValid(target.tile))
                {
                    return CardValidationResult.Invalid("INVALID_TILE", "Tile is outside board bounds.");
                }

                if (context.Board.IsTileOccupied(target.tile))
                {
                    return CardValidationResult.Invalid("TILE_OCCUPIED", "Tile is occupied.");
                }
            }

            //Ali : si la carte est un Character, la case doit être dans la zone de déploiement du joueur
            if (sourceCard.SourceCard is CharacterCardData)
            {
                HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();

                if (!BoardPlacementRules.CanPlaceCharacter(target.tile, context.ActingPlayerKey, grid))
                {
                    return CardValidationResult.Invalid(
                        "OUTSIDE_DEPLOYMENT_ZONE",
                        "Character must be placed in your deployment zone.");
                }
            }

            // Ali: World Effect fallback must follow the same owner-half rule.
            if (sourceCard.SourceCard is WorldEffectCardData)
            {
                HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();

                if (!BoardPlacementRules.CanPlaceWorldEffect(target.tile, context.ActingPlayerKey, grid))
                {
                    return CardValidationResult.Invalid(
                        "OUTSIDE_OWNER_HALF",
                        "World Effect must be placed in the owner's half.");
                }
            }

            // Ali: spells do not target empty tiles in v1.
            if (sourceCard.SourceCard is SpellCardData)
            {
                return CardValidationResult.Invalid(
                    "WRONG_TARGET_TYPE",
                    "Spell cards do not target empty tiles by default in v1.");
            }

            return CardValidationResult.Valid();
        }

        if (target.type == CardTargetType.EnemyFort)
        {
            // Ali: only damage spells can target the enemy Fort.
            if (sourceCard.SourceCard is SpellCardData spellCard && spellCard.effectType != SpellEffectType.Damage)
            {
                return CardValidationResult.Invalid(
                    "WRONG_TARGET_TYPE",
                    "Only damage spells can target the enemy Fort.");
            }

            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Enemy fort target player is required.");
            }

            if (target.targetPlayerId != context.OpponentPlayerKey)
            {
                return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target player is not the current opponent.");
            }

            //Ali:
            HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
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

        if (target.type == CardTargetType.Player)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Player target id is missing.");
            }

            return CardValidationResult.Valid();
        }

        return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported.");
    }
}
