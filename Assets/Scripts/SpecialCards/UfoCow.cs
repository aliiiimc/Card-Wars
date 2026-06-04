public class UfoCow : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return unitCardData is UfoCowCardData;
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return 0;
    }

    public override void OnAfterSpawn(Unit unit, CharacterCardData unitCardData)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: true);
    }

    public override void OnOwnerTurnStart(Unit unit, CharacterCardData unitCardData)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: true);
    }

    public bool CanConsumeEnemyField(HexTile tile, string unitOwner)
    {
        return tile != null
            && tile.HasWorldEffect()
            && tile.isFieldTile
            && tile.worldEffectOwner != "none"
            && tile.worldEffectOwner != unitOwner;
    }

    public bool ConsumeOneFieldStep(Unit ufoCow, HexGrid grid, int consumeAmount = -1)
    {
        if (ufoCow == null || grid == null || ufoCow.currentTile == null)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        HexTile currentTile = ufoCow.currentTile;
        if (!CanConsumeEnemyField(currentTile, ufoCow.owner))
        {
            return false;
        }
        int configuredConsumeAmount = 1;
        if (ufoCow.sourceCharacterCardData is UfoCowCardData ufoCowCardData)
        {
            configuredConsumeAmount = UnityEngine.Mathf.Max(1, ufoCowCardData.fieldConsumeAmount);
        }
        int resolvedConsumeAmount = consumeAmount > 0 ? consumeAmount : configuredConsumeAmount;
        int safeConsume = UnityEngine.Mathf.Max(1, resolvedConsumeAmount);

        if (worldEffectManager.TryDamageField(currentTile, safeConsume))
        {
            UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Consumed field tile at ({currentTile.coord.q},{currentTile.coord.r}).");
            return true;
        }

        return false;
    }

    public override void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
        ExecuteAutoFieldRoutine(unit, refreshTurnStartTile: false);
    }

    private void ExecuteAutoFieldRoutine(Unit unit, bool refreshTurnStartTile)
    {
        if (unit == null)
        {
            return;
        }

        HexGrid grid = UnityEngine.Object.FindFirstObjectByType<HexGrid>();
        if (grid == null)
        {
            return;
        }

        bool consumed = ConsumeOneFieldStep(unit, grid);
        if (!consumed && TryMoveOntoAdjacentEnemyField(unit, grid, refreshTurnStartTile))
        {
            consumed = ConsumeOneFieldStep(unit, grid);
        }

        if (!consumed)
        {
            UnityEngine.Debug.Log("[SpecialTrigger][UfoCow] No enemy field tile consumed.");
        }
    }

    private bool TryMoveOntoAdjacentEnemyField(Unit unit, HexGrid grid, bool refreshTurnStartTile)
    {
        if (unit == null || grid == null || unit.currentTile == null)
        {
            return false;
        }

        System.Collections.Generic.List<HexTile> neighbors = HexUtils.GetNeighbors(unit.currentTile, grid);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexTile neighbor = neighbors[i];
            if (!CanConsumeEnemyField(neighbor, unit.owner) || !neighbor.CanUnitOccupy())
            {
                continue;
            }

            HexTile previousTile = unit.currentTile;
            previousTile.ClearUnitOccupant();
            unit.PlaceOnTile(neighbor);
            if (refreshTurnStartTile)
            {
                unit.turnStartTile = neighbor;
            }

            UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Moved onto enemy field tile at ({neighbor.coord.q},{neighbor.coord.r}).");
            return true;
        }

        return false;
    }
}
