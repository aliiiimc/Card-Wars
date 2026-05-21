using UnityEngine;

[CreateAssetMenu(fileName = "WallCard", menuName = "Cards/Special/Wall")]
public class WallCardData : WorldEffectCardData
{
    [Header("Wall Balancing")]
    [Min(1)]
    public int tilesPerWall = 3;
}
