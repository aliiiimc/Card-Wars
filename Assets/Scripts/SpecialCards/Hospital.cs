using UnityEngine;

public class Hospital
{
    private const int DefaultHealAmount = 1;
    private const int DefaultTriggerRange = 1;

    public int ApplyAutomaticHealing(string ownerKey)
    {
        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            return 0;
        }

        WorldEffect[] allWorldEffects = Object.FindObjectsByType<WorldEffect>(FindObjectsSortMode.None);
        Unit[] allUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
        int totalHealing = 0;
        int hospitalCount = 0;
        SpellManager spellManager = SpellManager.GetOrCreate();

        for (int i = 0; i < allWorldEffects.Length; i++)
        {
            WorldEffect worldEffect = allWorldEffects[i];
            if (!TryGetHospitalProfile(worldEffect, out int healAmount, out int triggerRange))
            {
                continue;
            }

            if (spellManager != null && spellManager.IsWorldEffectDisabled(worldEffect))
            {
                continue;
            }

            if (worldEffect.currentTile == null || worldEffect.owner != ownerKey)
            {
                continue;
            }

            bool healedFromThisHospital = false;
            for (int j = 0; j < allUnits.Length; j++)
            {
                Unit unit = allUnits[j];
                if (unit == null || unit.currentTile == null || unit.owner != ownerKey)
                {
                    continue;
                }

                int distance = HexUtils.GetHexDistance(worldEffect.currentTile, unit.currentTile);
                if (distance < 0 || distance > triggerRange)
                {
                    continue;
                }

                int healthBefore = unit.health;
                unit.ApplyHeal(healAmount);
                int healedAmount = Mathf.Max(0, unit.health - healthBefore);
                if (healedAmount <= 0)
                {
                    continue;
                }

                totalHealing += healedAmount;
                healedFromThisHospital = true;
            }

            if (healedFromThisHospital)
            {
                hospitalCount++;
            }
        }

        if (totalHealing > 0)
        {
            Debug.Log(
                $"[SpecialTrigger][Hospital] '{ownerKey}' healed {totalHealing} total HP from {hospitalCount} hospital(s).");
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

        if (!worldEffect.sourceCard.SourceCard.MatchesSpecialCard(string.Empty, "Hospital"))
        {
            return false;
        }

        healAmount = DefaultHealAmount;
        triggerRange = DefaultTriggerRange;
        return true;
    }
}
