using UnityEngine;

public sealed class Sabotage
{
    private const string CardName = "Sabotage";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellSabotage, CardName);
    }

    public CardEffectResult Apply(CardRuntimeState sourceCard, CardTarget target)
    {
        if (!(sourceCard?.SourceCard is SpellCardData spellCard))
        {
            return CardEffectResult.Failure("NO_SABOTAGE_SPELL", "Sabotage needs a spell card source.");
        }

        if (target.targetCard == null || !(target.targetCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return CardEffectResult.Failure("NO_TARGET_STRUCTURE", "Sabotage needs an enemy building target.");
        }

        if (worldEffectCard.category != WorldEffectCategory.Structure)
        {
            return CardEffectResult.Failure("WRONG_TARGET_STRUCTURE", "Sabotage can only target enemy buildings.");
        }

        HexTile targetTile = FindTargetTile(target.tile);
        if (targetTile == null || !targetTile.HasWorldEffect())
        {
            return CardEffectResult.Failure("STRUCTURE_NOT_PRESENT", "Sabotage needs a real enemy building on the board.");
        }

        int durationTurns = Mathf.Max(0, spellCard.effectDurationTurns);
        if (durationTurns <= 0)
        {
            return CardEffectResult.Failure("INVALID_DURATION", "Sabotage needs effectDurationTurns above zero.");
        }

        return CardEffectResult.Success($"Target building disabled for {durationTurns} turn(s).");
    }

    public bool MatchesWorldEffect(Spell spell, WorldEffect worldEffect)
    {
        if (spell == null || worldEffect == null || worldEffect.currentTile == null)
        {
            return false;
        }

        HexTile targetTile = FindTargetTile(spell.target.tile);
        if (targetTile == null)
        {
            return false;
        }

        return targetTile.coord.q == worldEffect.currentTile.coord.q
            && targetTile.coord.r == worldEffect.currentTile.coord.r;
    }

    private static HexTile FindTargetTile(AxialCoord coord)
    {
        HexGrid grid = Object.FindFirstObjectByType<HexGrid>();
        return grid != null ? grid.GetTile(coord) : null;
    }
}
