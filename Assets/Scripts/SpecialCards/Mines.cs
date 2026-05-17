using UnityEngine;

public class Mines
{
    public int GetMinesToPlace(MinesCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 5;
        }

        return Mathf.Max(1, worldEffectCard.minesToPlace);
    }

    public int GetMineDamage(MinesCardData worldEffectCard)
    {
        if (worldEffectCard == null)
        {
            return 3;
        }

        return Mathf.Max(1, worldEffectCard.mineDamage);
    }

    public string GetEnemyWarningMessage()
    {
        return "Warning: potential mines detected in this area.";
    }
}
