using UnityEngine;

public sealed class TaxCollection
{
    private const string CardName = "Tax collection";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellTaxCollection, CardName);
    }

    public CardEffectResult Apply(CardRuntimeState sourceCard, CardTarget target)
    {
        if (!(sourceCard?.SourceCard is SpellCardData spellCard))
        {
            return CardEffectResult.Failure("NO_TAX_COLLECTION_SPELL", "Tax collection needs a spell card source.");
        }

        if (target.targetCard == null || !(target.targetCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return CardEffectResult.Failure("NO_TARGET_FIELD", "Tax collection needs a money-generating field target.");
        }

        if (worldEffectCard.category != WorldEffectCategory.ResourceField)
        {
            return CardEffectResult.Failure("WRONG_TARGET_FIELD", "Tax collection can only target money-generating fields.");
        }

        HexTile targetTile = FindTargetTile(target.tile);
        if (targetTile == null || !targetTile.HasWorldEffect() || !targetTile.isFieldTile)
        {
            return CardEffectResult.Failure("FIELD_NOT_PRESENT", "Tax collection needs a real field tile on the board.");
        }

        int durationTurns = Mathf.Max(0, spellCard.effectDurationTurns);
        if (durationTurns <= 0)
        {
            return CardEffectResult.Failure("INVALID_DURATION", "Tax collection needs effectDurationTurns above zero.");
        }

        return CardEffectResult.Success($"Field income will be redirected for {durationTurns} turn(s).");
    }

    public bool MatchesIncomeTile(Spell spell, HexTile incomeTile)
    {
        if (spell == null || incomeTile == null || !incomeTile.HasWorldEffect() || !incomeTile.isFieldTile)
        {
            return false;
        }

        HexTile targetTile = FindTargetTile(spell.target.tile);
        if (targetTile == null || !targetTile.HasWorldEffect() || !targetTile.isFieldTile)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(targetTile.fieldClusterId) && !string.IsNullOrWhiteSpace(incomeTile.fieldClusterId))
        {
            return targetTile.fieldClusterId == incomeTile.fieldClusterId;
        }

        return targetTile.coord.q == incomeTile.coord.q && targetTile.coord.r == incomeTile.coord.r;
    }

    private static HexTile FindTargetTile(AxialCoord coord)
    {
        HexGrid grid = Object.FindFirstObjectByType<HexGrid>();
        return grid != null ? grid.GetTile(coord) : null;
    }
}
