using System.Collections.Generic;
using UnityEngine;

public static class HexUtils
{
    // Neighbor offsets for even rows (r % 2 == 0)
    private static int[][] evenOffsets = new int[][]
    {
        new int[] {+1,  0}, // right
        new int[] {-1,  0}, // left
        new int[] { 0, +1}, // top-right
        new int[] {-1, +1}, // top-left
        new int[] { 0, -1}, // bottom-right
        new int[] {-1, -1}  // bottom-left
    };

    // Neighbor offsets for odd rows (r % 2 == 1)
    private static int[][] oddOffsets = new int[][]
    {
        new int[] {+1,  0}, // right
        new int[] {-1,  0}, // left
        new int[] {+1, +1}, // top-right
        new int[] { 0, +1}, // top-left
        new int[] {+1, -1}, // bottom-right
        new int[] { 0, -1}  // bottom-left
    };

    public static List<HexTile> GetNeighbors(HexTile tile, HexGrid grid)
    {
        List<HexTile> neighbors = new List<HexTile>();
        int[][] offsets = (tile.r % 2 == 0) ? evenOffsets : oddOffsets;

        foreach (int[] offset in offsets)
        {
            HexTile neighbor = grid.GetTile(tile.q + offset[0], tile.r + offset[1]);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }
        return neighbors;
    }

    public static List<HexTile> GetTilesInRange(HexTile start, int range, HexGrid grid)
    {
        List<HexTile> visited = new List<HexTile>();
        List<HexTile> frontier = new List<HexTile>();

        visited.Add(start);
        frontier.Add(start);

        for (int step = 0; step < range; step++)
        {
            List<HexTile> nextFrontier = new List<HexTile>();

            foreach (HexTile tile in frontier)
            {
                foreach (HexTile neighbor in GetNeighbors(tile, grid))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        nextFrontier.Add(neighbor);
                    }
                }
            }
            frontier = nextFrontier;
        }

        visited.Remove(start);
        return visited;
    }
}