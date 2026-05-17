using UnityEngine;

[CreateAssetMenu(fileName = "ArcherCard", menuName = "Cards/Special/Archer")]
public class ArcherCardData : CharacterCardData
{
    [Header("Archer Balancing")]
    public int bonusAttackRange = 2;
}
