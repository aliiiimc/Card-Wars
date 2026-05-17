using UnityEngine;

[CreateAssetMenu(fileName = "MinerCard", menuName = "Cards/Special/Miner")]
public class MinerCardData : CharacterCardData
{
    [Header("Miner Balancing")]
    [Range(0f, 1f)]
    public float movingVisibilityAlpha = 0.3f;
}
