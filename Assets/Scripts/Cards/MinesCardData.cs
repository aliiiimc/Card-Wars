using UnityEngine;

[CreateAssetMenu(fileName = "MinesCard", menuName = "Cards/Special/Mines")]
public class MinesCardData : WorldEffectCardData
{
    [Header("Mines Balancing")]
    public int minesToPlace = 5;
    public int mineDamage = 3;
}
