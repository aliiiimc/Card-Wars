using UnityEngine;

public static class BoardPlacementRules // verify if a board-placement card can be put on the board.
{
    public static bool CanPlaceCharacter(AxialCoord coord, string playerKey, HexGrid grid, CharacterCardData characterCard = null)
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

        if (CanPlaceUfoCowOnField(characterCard, tile, playerKey))
        {
            return true;
        }

        // (abdo :) Movement may allow standing on some world effects, but normal card spawns still need a plain empty tile.
        if (tile.HasUnitOccupant() || tile.tileType == "fort" || tile.HasWorldEffect() || !tile.CanUnitOccupy())
        {
            return false;
        }

        if (grid.IsInPlayerDeploymentZone(coord, playerKey))
        {
            return true;
        }

        // Camp special rule: when active, it opens a new spawn location around owned camp tiles.
        Camp camp = new Camp();
        CampCardData campCardData = ResolveCampCardData();
        if (!camp.ForcesNewSpawnLocation(campCardData))
        {
            return false;
        }

        return camp.CanOpenSpawnLocation(tile, playerKey, grid, campCardData);
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

        if (!tile.CanPlaceWorldEffect())
        {
            return false;
        }

        return grid.IsInPlayerHalf(coord, playerKey);
    }

    private static CampCardData ResolveCampCardData()
    {
        CardLibrary[] libraries = Object.FindObjectsByType<CardLibrary>(FindObjectsSortMode.None);
        for (int i = 0; i < libraries.Length; i++)
        {
            CardLibrary library = libraries[i];
            if (library == null || library.cards == null)
            {
                continue;
            }

            for (int j = 0; j < library.cards.Count; j++)
            {
                if (!(library.cards[j] is CampCardData campCardData))
                {
                    continue;
                }

                return campCardData;
            }
        }

        return null;
    }

    private static bool CanPlaceUfoCowOnField(CharacterCardData characterCard, HexTile tile, string playerKey)
    {
        if (characterCard == null || tile == null)
        {
            return false;
        }

        if (!string.Equals(characterCard.DisplayName, "UFO Cow", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return tile.HasWorldEffect()
            && tile.isFieldTile
            && tile.worldEffectOwner != "none"
            && tile.worldEffectOwner != playerKey
            && tile.CanUnitOccupy();
    }
}
