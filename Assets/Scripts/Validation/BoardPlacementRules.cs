public static class BoardPlacementRules //verify if x card can be put on the board.
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

        return grid.IsInPlayerDeploymentZone(coord, playerKey);
    }
}
