using System;
using UnityEngine;

// Axial coordinate for hex grids: q (column axis) and r (row axis).
[Serializable]
public struct AxialCoord : IEquatable<AxialCoord>
{
    public int q;
    public int r;

    public AxialCoord(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    public static AxialCoord Zero => new AxialCoord(0, 0);

    // Helper for transitioning from old Vector2Int-based calls.
    public static AxialCoord FromVector2Int(Vector2Int value)
    {
        return new AxialCoord(value.x, value.y);
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(q, r);
    }

    public bool Equals(AxialCoord other)
    {
        return q == other.q && r == other.r;
    }

    public override bool Equals(object obj)
    {
        return obj is AxialCoord other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (q * 397) ^ r;
        }
    }

    public override string ToString()
    {
        return $"({q}, {r})";
    }
}
