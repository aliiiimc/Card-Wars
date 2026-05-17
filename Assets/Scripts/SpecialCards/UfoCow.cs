public class UfoCow : SpecialCardScriptBase
{
    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "UFO Cow");
    }

    public bool CanSpawnOnField(HexTile tile)
    {
        return tile != null && tile.tileType == "worldEffect" && tile.isFieldTile;
    }

    public bool ConsumeOneFieldStep(Unit ufoCow, HexGrid grid, int consumeAmount = 1)
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
        if (!CanSpawnOnField(currentTile))
        {
            return false;
        }

        string clusterId = currentTile.fieldClusterId;
        int safeConsume = UnityEngine.Mathf.Max(1, consumeAmount);

        // Prefer consuming the current tile first.
        if (worldEffectManager.TryDamageField(currentTile, safeConsume))
        {
            UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Consumed field tile at ({currentTile.coord.q},{currentTile.coord.r}).");
            return true;
        }

        // Then consume an adjacent tile in the same field cluster.
        System.Collections.Generic.List<HexTile> neighbors = HexUtils.GetNeighbors(currentTile, grid);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexTile neighbor = neighbors[i];
            if (neighbor == null
                || !neighbor.isFieldTile
                || neighbor.tileType != "worldEffect"
                || neighbor.fieldClusterId != clusterId)
            {
                continue;
            }

            if (worldEffectManager.TryDamageField(neighbor, safeConsume))
            {
                UnityEngine.Debug.Log($"[SpecialTrigger][UfoCow] Consumed adjacent field tile at ({neighbor.coord.q},{neighbor.coord.r}).");
                return true;
            }
        }

        return false;
    }

    public override void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
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

        bool consumed = ConsumeOneFieldStep(unit, grid, 1);
        if (!consumed)
        {
            UnityEngine.Debug.Log("[SpecialTrigger][UfoCow] No field tile consumed after move.");
        }
    }
}
