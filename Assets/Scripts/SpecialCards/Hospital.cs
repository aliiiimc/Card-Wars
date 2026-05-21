using UnityEngine;

public class Hospital
{
    private const int DefaultHealAmount = 1;
    private const int DefaultTriggerRange = 1;

    public int ApplyAdjacentHospitalHealing(Unit movedUnit)
    {
        if (movedUnit == null || movedUnit.currentTile == null || string.IsNullOrWhiteSpace(movedUnit.owner))
        {
            return 0;
        }

        WorldEffect[] allWorldEffects = Object.FindObjectsByType<WorldEffect>(FindObjectsSortMode.None);
        int totalHealing = 0;
        int hospitalCount = 0;

        for (int i = 0; i < allWorldEffects.Length; i++)
        {
            WorldEffect worldEffect = allWorldEffects[i];
            if (!TryGetHospitalProfile(worldEffect, out int healAmount, out int triggerRange))
            {
                continue;
            }

            if (worldEffect.currentTile == null || worldEffect.owner != movedUnit.owner)
            {
                continue;
            }

            int distance = HexUtils.GetHexDistance(worldEffect.currentTile, movedUnit.currentTile);
            if (distance < 0 || distance > triggerRange)
            {
                continue;
            }

            movedUnit.ApplyHeal(healAmount);
            totalHealing += healAmount;
            hospitalCount++;
        }

        if (totalHealing > 0)
        {
            Debug.Log(
                $"[SpecialTrigger][Hospital] {movedUnit.name} healed {totalHealing} total HP from {hospitalCount} adjacent hospital(s).");
        }

        return totalHealing;
    }

    private static bool TryGetHospitalProfile(WorldEffect worldEffect, out int healAmount, out int triggerRange)
    {
        healAmount = 0;
        triggerRange = 0;

        if (worldEffect == null || worldEffect.sourceCard == null || worldEffect.sourceCard.SourceCard == null)
        {
            return false;
        }

        if (worldEffect.sourceCard.SourceCard is HospitalCardData hospitalCard)
        {
            healAmount = Mathf.Max(1, hospitalCard.healAmount);
            triggerRange = Mathf.Max(1, hospitalCard.triggerRange);
            return true;
        }

        if (!string.Equals(worldEffect.sourceCard.SourceCard.DisplayName, "Hospital", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        healAmount = DefaultHealAmount;
        triggerRange = DefaultTriggerRange;
        return true;
    }
}
