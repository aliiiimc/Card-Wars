using UnityEngine;

[CreateAssetMenu(fileName = "DragonCard", menuName = "Cards/Special/Dragon")]
public class DragonCardData : CharacterCardData
{
    [Header("Dragon Balancing")]
    public int bonusAttackRange = 2;

    [Header("Dragon Visuals")]
    public ProjectileVisualSettings projectileVisuals = new ProjectileVisualSettings();
}
