public static class BoardPlacementRules // verify if a board-placement card can be put on the board.
{
    public static bool CanPlaceCharacter(AxialCoord coord, string playerKey, HexGrid grid)
    {
        if (grid == null)
        {
            return false;
        }

        HexTile tile = grid.GetTile(coord);
        if (tile == null)
        {
            return false;
        }

        if (!tile.IsEmpty())
        {
            return false;
        }

        if (grid.IsInPlayerDeploymentZone(coord, playerKey))
        {
            return true;
        }

        // Camp special rule: when active, it opens a new spawn location around owned camp tiles.
        Camp camp = new Camp();
        if (!camp.ForcesNewSpawnLocation())
        {
            return false;
        }

        return IsAdjacentToOwnedCamp(tile, playerKey, grid);
    }

    // Ali: keep World Effect placement in one shared helper so player and AI validation cannot drift.
    public static bool CanPlaceWorldEffect(AxialCoord coord, string playerKey, HexGrid grid)
    {
        if (grid == null)
        {
            return false;
        }

        HexTile tile = grid.GetTile(coord);
        if (tile == null)
        {
            return false;
        }

        if (!tile.IsEmpty())
        {
            return false;
        }

        return grid.IsInPlayerHalf(coord, playerKey);
    }

    private static bool IsAdjacentToOwnedCamp(HexTile tile, string playerKey, HexGrid grid)
    {
        if (tile == null || grid == null || string.IsNullOrWhiteSpace(playerKey))
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

            if (neighbor.tileType == "worldEffect"
                && neighbor.isCampTile
                && neighbor.owner == playerKey)
            {
                return true;
            }
        }

        return false;
    }
}
