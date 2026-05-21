public class Camp
{
    public bool ForcesNewSpawnLocation(CampCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return true;
        }

        return worldEffectCard.forcesNewSpawnLocation;
    }

    public bool TryActivateOnTile(WorldEffectManager worldEffectManager, HexTile tile, CampCardData worldEffectCard)
    {
        if (!ForcesNewSpawnLocation(worldEffectCard) || worldEffectManager == null || tile == null)
        {
            return false;
        }

        return worldEffectManager.TrySetCampData(tile);
    }

    public bool CanOpenSpawnLocation(HexTile tile, string playerKey, HexGrid grid, CampCardData worldEffectCard)
    {
        if (!ForcesNewSpawnLocation(worldEffectCard) || tile == null || grid == null || string.IsNullOrWhiteSpace(playerKey))
        {
            return false;
        }

        var neighbors = HexUtils.GetNeighbors(tile, grid);
        for (int i = 0; i < neighbors.Count; i++)
        {
            HexTile neighbor = neighbors[i];
            if (neighbor == null)
            {
                continue;
            }

            if (neighbor.HasWorldEffect()
                && neighbor.isCampTile
                && neighbor.worldEffectOwner == playerKey)
            {
                return true;
            }
        }

        return false;
    }
}
