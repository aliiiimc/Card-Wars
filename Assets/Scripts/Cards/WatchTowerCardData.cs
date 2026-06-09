using UnityEngine;

[CreateAssetMenu(fileName = "WatchTowerCard", menuName = "Cards/Special/Watch Tower")]
public class WatchTowerCardData : WorldEffectCardData
{
    [Header("Watch Tower Balancing")]
    public int bonusAttackRange = 0;

    [Header("Watch Tower Visuals")]
    public ProjectileVisualSettings projectileVisuals = new ProjectileVisualSettings();
}
