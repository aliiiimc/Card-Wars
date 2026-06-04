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

        if (sourceCard.SourceCard is SpellCardData spellCard)
        {
            if (spellCard.MatchesSpecialCard(SpecialCardIds.SpellSabotage, "Sabotage"))
            {
                return ValidateSabotageTarget(context, target);
            }

            if (spellCard.MatchesSpecialCard(SpecialCardIds.SpellTaxCollection, "Tax collection"))
            {
                return ValidateTaxCollectionTarget(context, target);
            }

            CardValidationResult spellRuleResult = ValidateSpellTargetRules(spellCard, target);
            if (!spellRuleResult.IsValid)
            {
                return spellRuleResult;
            }
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
            if (sourceCard.SourceCard is CharacterCardData characterCard)
            {
                HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();

                if (!BoardPlacementRules.CanPlaceCharacter(target.tile, context.ActingPlayerKey, grid, characterCard))
                {
                    return CardValidationResult.Invalid(
                        "OUTSIDE_DEPLOYMENT_ZONE",
                        "Character must be placed in your deployment zone.");
                }
            }
            
            // Ali: spells do not target empty tiles in v1.
            if (sourceCard.SourceCard is SpellCardData)
            {
                return CardValidationResult.Invalid(
                    "WRONG_TARGET_TYPE",
                    "Spell cards do not target empty tiles by default in v1.");
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

            

            return CardValidationResult.Valid();
        }

        if (target.type == CardTargetType.AllyUnit || target.type == CardTargetType.EnemyUnit)
        {
            return ValidateUnitTarget(context, target, shouldBeAlly: target.type == CardTargetType.AllyUnit);
        }

        if (target.type == CardTargetType.AllyStructure || target.type == CardTargetType.EnemyStructure)
        {
            return ValidateStructureTarget(context, target, shouldBeAlly: target.type == CardTargetType.AllyStructure);
        }

        if (target.type == CardTargetType.EnemyFort)
        {
            // Ali: only damage spells can target the enemy Fort.
            if (sourceCard.SourceCard is SpellCardData damageSpellCard && damageSpellCard.effectType != SpellEffectType.Damage)
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

            //Ali: fallback validation must check the real Fort on the real tile.
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

        if (target.type == CardTargetType.AllyFort)
        {
            if (string.IsNullOrWhiteSpace(target.targetPlayerId))
            {
                return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Ally fort target player is required.");
            }

            if (target.targetPlayerId != context.ActingPlayerKey)
            {
                return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target player is not the current player.");
            }

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

    private static CardValidationResult ValidateSpellTargetRules(SpellCardData spellCard, CardTarget target)
    {
        switch (spellCard.effectType)
        {
            case SpellEffectType.Buff:
            case SpellEffectType.Boost:
                return target.type == CardTargetType.AllyUnit
                    ? CardValidationResult.Valid()
                    : CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Buff and boost spells can only target allied units.");

            case SpellEffectType.Heal:
                return target.type == CardTargetType.AllyUnit || target.type == CardTargetType.AllyFort
                    ? CardValidationResult.Valid()
                    : CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Heal spells can only target allied units or the allied fort.");

            case SpellEffectType.Damage:
                return target.type == CardTargetType.EnemyUnit
                    || target.type == CardTargetType.EnemyStructure
                    || target.type == CardTargetType.EnemyFort
                    ? CardValidationResult.Valid()
                    : CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Damage spells can only target enemy units, structures, or the enemy fort.");

            case SpellEffectType.Debuff:
                return target.type == CardTargetType.EnemyUnit
                    ? CardValidationResult.Valid()
                    : CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Debuff spells can only target enemy units.");

            case SpellEffectType.Utility:
                return target.type == CardTargetType.AllyUnit
                    ? CardValidationResult.Valid()
                    : CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Utility spells can only target allied units in v1.");

            case SpellEffectType.Summon:
                return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Summon-type spells are not part of the current v1 spell targeting rules.");

            default:
                return CardValidationResult.Invalid("UNSUPPORTED_SPELL_EFFECT", $"Spell effect type '{spellCard.effectType}' is not supported.");
        }
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

    private static CardValidationResult ValidateStructureTarget(CardValidationContext context, CardTarget target, bool shouldBeAlly)
    {
        if (target.targetCard == null)
        {
            return CardValidationResult.Invalid("NO_TARGET_CARD", "Structure target requires target card.");
        }

        if (!(target.targetCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return CardValidationResult.Invalid("NOT_STRUCTURE", "Target card is not a structure.");
        }

        if (worldEffectCard.category != WorldEffectCategory.Structure)
        {
            return CardValidationResult.Invalid("NOT_STRUCTURE", "Target world effect is not a structure.");
        }

        if (!target.targetCard.IsManifestedOnBoard)
        {
            return CardValidationResult.Invalid("NOT_ON_BOARD", "Target structure is not on the board.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId))
        {
            return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Structure target player id is required.");
        }

        string expected = shouldBeAlly ? context.ActingPlayerKey : context.OpponentPlayerKey;
        if (target.targetPlayerId != expected)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", shouldBeAlly
                ? "Target structure is not allied."
                : "Target structure is not an enemy.");
        }

        return CardValidationResult.Valid();
    }

    private static CardValidationResult ValidateTaxCollectionTarget(CardValidationContext context, CardTarget target)
    {
        if (target.type != CardTargetType.EnemyStructure)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Tax collection can only target enemy money-generating fields.");
        }

        if (target.targetCard == null || !(target.targetCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return CardValidationResult.Invalid("NO_TARGET_CARD", "Tax collection needs a field target.");
        }

        if (worldEffectCard.category != WorldEffectCategory.ResourceField)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_FIELD", "Tax collection can only target money-generating fields.");
        }

        if (!target.targetCard.IsManifestedOnBoard)
        {
            return CardValidationResult.Invalid("NOT_ON_BOARD", "Target field is not on the board.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId) || target.targetPlayerId != context.OpponentPlayerKey)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target field is not owned by the opponent.");
        }

        HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        HexTile targetTile = grid != null ? grid.GetTile(target.tile) : null;
        if (targetTile == null || !targetTile.HasWorldEffect() || !targetTile.isFieldTile)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_FIELD", "Tax collection needs a real field tile target.");
        }

        return CardValidationResult.Valid();
    }

    private static CardValidationResult ValidateSabotageTarget(CardValidationContext context, CardTarget target)
    {
        if (target.type != CardTargetType.EnemyStructure)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Sabotage can only target enemy buildings.");
        }

        if (target.targetCard == null || !(target.targetCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return CardValidationResult.Invalid("NO_TARGET_CARD", "Sabotage needs an enemy building target.");
        }

        if (worldEffectCard.category != WorldEffectCategory.Structure)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_STRUCTURE", "Sabotage can only target enemy buildings.");
        }

        if (!target.targetCard.IsManifestedOnBoard)
        {
            return CardValidationResult.Invalid("NOT_ON_BOARD", "Target building is not on the board.");
        }

        if (string.IsNullOrWhiteSpace(target.targetPlayerId) || target.targetPlayerId != context.OpponentPlayerKey)
        {
            return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target building is not owned by the opponent.");
        }

        HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        HexTile targetTile = grid != null ? grid.GetTile(target.tile) : null;
        if (targetTile == null || !targetTile.HasWorldEffect())
        {
            return CardValidationResult.Invalid("WRONG_TARGET_STRUCTURE", "Sabotage needs a real enemy building target.");
        }

        return CardValidationResult.Valid();
    }
}
