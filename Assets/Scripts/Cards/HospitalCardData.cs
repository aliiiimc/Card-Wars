using UnityEngine;

[CreateAssetMenu(fileName = "HospitalCard", menuName = "Cards/Special/Hospital")]
public class HospitalCardData : WorldEffectCardData
{
    [Header("Hospital Balancing")]
    public int healAmount = 1;
    public int triggerRange = 1;
}
