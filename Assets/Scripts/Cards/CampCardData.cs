using UnityEngine;

[CreateAssetMenu(fileName = "CampCard", menuName = "Cards/Special/Camp")]
public class CampCardData : WorldEffectCardData
{
    [Header("Camp Balancing")]
    public bool forcesNewSpawnLocation = true;
}
