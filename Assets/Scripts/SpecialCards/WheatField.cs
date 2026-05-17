using System;
using System.Collections.Generic;
using UnityEngine;

public class WheatField
{
    private const int DefaultTilesPerField = 6;
    private const int DefaultHpPerTile = 1;

    public int GetBonusMoneyPerTurn()
    {
        return 1;
    }

    public string CreateClusterId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public List<HexTile> BuildFieldTiles(HexGrid grid, HexTile originTile, int requestedTileCount = DefaultTilesPerField)
    {
        List<HexTile> fieldTiles = new List<HexTile>();
        if (grid == null || originTile == null)
        {
            return fieldTiles;
        }

        int targetCount = Mathf.Max(1, requestedTileCount);

        Queue<HexTile> frontier = new Queue<HexTile>();
        HashSet<HexTile> visited = new HashSet<HexTile>();
        frontier.Enqueue(originTile);
        visited.Add(originTile);

        while (frontier.Count > 0 && fieldTiles.Count < targetCount)
        {
            HexTile current = frontier.Dequeue();
            if (current == null)
            {
                continue;
            }

            if (current.IsEmpty() || current.tileType == "worldEffect")
            {
                fieldTiles.Add(current);
            }

            List<HexTile> neighbors = HexUtils.GetNeighbors(current, grid);
            for (int i = 0; i < neighbors.Count; i++)
            {
                HexTile neighbor = neighbors[i];
                if (neighbor == null || visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                frontier.Enqueue(neighbor);
            }
        }

        return fieldTiles;
    }

    public bool ApplyFieldCluster(HexGrid grid, HexTile originTile, string owner, Sprite fieldSprite, CardRuntimeState sourceCard, out string clusterId, int tileCount = DefaultTilesPerField, int hpPerTile = DefaultHpPerTile)
    {
        clusterId = string.Empty;
        if (grid == null || originTile == null || string.IsNullOrWhiteSpace(owner) || sourceCard == null)
        {
            return false;
        }

        WorldEffectManager worldEffectManager = UnityEngine.Object.FindFirstObjectByType<WorldEffectManager>();
        if (worldEffectManager == null)
        {
            return false;
        }

        List<HexTile> tiles = BuildFieldTiles(grid, originTile, tileCount);
        if (tiles.Count == 0)
        {
            return false;
        }

        clusterId = CreateClusterId();
        int safeHpPerTile = Mathf.Max(1, hpPerTile);
        int placedTileCount = 0;

        for (int i = 0; i < tiles.Count; i++)
        {
            HexTile tile = tiles[i];
            bool placed = false;
            if (tile.IsEmpty())
            {
                placed = worldEffectManager.TryPlaceFromCard(tile, owner, sourceCard, out _);
            }
            else if (tile.tileType == "worldEffect")
            {
                bool ownershipReady = tile.owner == owner || worldEffectManager.TryColonize(tile, owner);
                placed = ownershipReady && worldEffectManager.TryReplace(tile, owner, sourceCard, out _);
            }

            if (!placed)
            {
                continue;
            }

            if (worldEffectManager.TrySetFieldData(tile, clusterId, safeHpPerTile))
            {
                placedTileCount++;
            }
        }

        Debug.Log($"[SpecialTrigger][WheatField] Cluster '{clusterId}' created with {placedTileCount} tile(s) for owner '{owner}'.");

        return true;
    }
}
