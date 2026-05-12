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

        // Ali: enforce spell-specific target rules so each spell effect type can only hit its allowed target category.
        if (sourceCard.SourceCard is SpellCardData spellCard)
        {
            CardValidationResult spellRuleResult = ValidateSpellTargetRules(spellCard, target);
            if (!spellRuleResult.IsValid)
            {
                return spellRuleResult;
            }
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

        // Ali: reuse the shared World Effect placement helper so player and AI rules stay identical.  
        //World Effect cards must target an empty tile inside the acting player's half of the board.
        if (sourceCard.SourceCard is WorldEffectCardData)
        {
            if (!BoardPlacementRules.CanPlaceWorldEffect(target.tile, context.ActingPlayerKey, grid))
            {
                HexTile targetTile = grid.GetTile(target.tile);
                if (targetTile != null && !targetTile.IsEmpty())
                {
                    return CardValidationResult.Invalid("TILE_OCCUPIED", "World Effect must be placed on an empty tile.");
                }

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



    // Ali: centralize spell targeting rules here so UI highlights and runtime validation stay aligned.
    private static CardValidationResult ValidateSpellTargetRules(SpellCardData spellCard, CardTarget target)
    {
        if (spellCard == null)
        {
            return CardValidationResult.Invalid("NO_SPELL_CARD", "Spell card data is missing.");
        }

        switch (spellCard.effectType)
        {
            case SpellEffectType.Buff:
            case SpellEffectType.Boost:
                if (target.type != CardTargetType.AllyUnit)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Buff and boost spells can only target allied units.");
                }
                return CardValidationResult.Valid();

            case SpellEffectType.Heal:
                if (target.type != CardTargetType.AllyUnit && target.type != CardTargetType.AllyFort)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Heal spells can only target allied units or the allied fort.");
                }
                return CardValidationResult.Valid();

            case SpellEffectType.Damage:
                if (target.type != CardTargetType.EnemyUnit && target.type != CardTargetType.EnemyFort)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Damage spells can only target enemy units or the enemy fort.");
                }
                return CardValidationResult.Valid();

            case SpellEffectType.Debuff:
                if (target.type != CardTargetType.EnemyUnit)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Debuff spells can only target enemy units.");
                }
                return CardValidationResult.Valid();

            case SpellEffectType.Utility:
                if (target.type != CardTargetType.AllyUnit)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_TYPE", "Utility spells can only target allied units in v1.");
                }
                return CardValidationResult.Valid();

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
