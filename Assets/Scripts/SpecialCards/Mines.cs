using UnityEngine;
using System.Collections.Generic;

public class Mines
{
    public int GetMinesToPlace(MinesCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 5;
        }

        return Mathf.Max(1, worldEffectCard.minesToPlace);
    }

    public int GetMineDamage(MinesCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 3;
        }

        return Mathf.Max(1, worldEffectCard.mineDamage);
    }

    public string GetEnemyWarningMessage()
    {
        return "Warning: potential mines detected in this area.";
    }

    public int ApplyMinefield(HexGrid grid, WorldEffectManager worldEffectManager, MinesCardData worldEffectCard, CardRuntimeState runtimeCard, string owner, HexTile anchorTile)
    {
        if (grid == null || worldEffectManager == null || worldEffectCard == null || runtimeCard == null || string.IsNullOrWhiteSpace(owner))
        {
            return 0;
        }

        if (anchorTile != null && anchorTile.HasWorldEffect())
        {
            worldEffectManager.Remove(anchorTile);
        }

        List<HexTile> candidates = GetRandomMineCandidates(grid, owner);
        int requestedCount = GetMinesToPlace(worldEffectCard);
        int mineDamage = GetMineDamage(worldEffectCard);
        int placedCount = 0;

        for (int i = 0; i < candidates.Count && placedCount < requestedCount; i++)
        {
            HexTile candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (!worldEffectManager.TryPlaceFromCard(candidate, owner, runtimeCard, out _))
            {
                continue;
            }

            if (worldEffectManager.TrySetMineData(candidate, mineDamage))
            {
                placedCount++;
            }
        }

        return placedCount;
    }

    public bool IsEnemyMineTileForUnit(HexTile tile, Unit unit)
    {
        return tile != null
            && unit != null
            && tile.HasWorldEffect()
            && tile.isMineTile
            && tile.worldEffectOwner != "none"
            && tile.worldEffectOwner != unit.owner;
    }

    public bool TryTriggerMine(Unit unit, HexTile tile, WorldEffectManager worldEffectManager, out int mineDamage)
    {
        mineDamage = 0;

        if (!IsEnemyMineTileForUnit(tile, unit))
        {
            return false;
        }

        mineDamage = Mathf.Max(1, tile.mineDamage);
        if (worldEffectManager != null)
        {
            worldEffectManager.Remove(tile);
        }
        else
        {
            tile.RemoveWorldEffect();
        }

        return true;
    }

    private List<HexTile> GetRandomMineCandidates(HexGrid grid, string owner)
    {
        List<HexTile> candidates = new List<HexTile>();
        HexTile[] allTiles = Object.FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        if (allTiles == null || allTiles.Length == 0 || grid == null)
        {
            return candidates;
        }

        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            if (tile == null || tile.tileType != "empty" || tile.HasWorldEffect())
            {
                continue;
            }

            if (!grid.IsInPlayerHalf(tile.coord, owner))
            {
                continue;
            }

            candidates.Add(tile);
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            HexTile temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        return candidates;
    }
}
