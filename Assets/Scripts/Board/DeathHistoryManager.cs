using System.Collections.Generic;
using UnityEngine;

public sealed class DeathHistoryManager : MonoBehaviour
{
    public sealed class CharacterDeathRecord
    {
        public CharacterCardData characterCard;
        public string ownerKey;
        public int turnNumber;
    }

    private readonly List<CharacterDeathRecord> characterDeaths = new List<CharacterDeathRecord>();

    public static DeathHistoryManager GetOrCreate()
    {
        DeathHistoryManager manager = FindFirstObjectByType<DeathHistoryManager>();
        if (manager != null)
        {
            return manager;
        }

        GameObject managerObject = new GameObject("DeathHistoryManager");
        return managerObject.AddComponent<DeathHistoryManager>();
    }

    public void RecordCharacterDeath(CardRuntimeState runtimeCard, string ownerKey)
    {
        if (!(runtimeCard?.SourceCard is CharacterCardData characterCard))
        {
            return;
        }

        characterDeaths.Add(new CharacterDeathRecord
        {
            characterCard = characterCard,
            ownerKey = ownerKey,
            turnNumber = ResolveCurrentTurnNumber()
        });
    }

    public List<CharacterCardData> GetRecentCharacterChoices(int lookbackTurns)
    {
        List<CharacterCardData> choices = new List<CharacterCardData>();
        HashSet<CharacterCardData> seenCards = new HashSet<CharacterCardData>();
        int safeLookbackTurns = Mathf.Max(0, lookbackTurns);
        int currentTurnNumber = ResolveCurrentTurnNumber();

        for (int i = characterDeaths.Count - 1; i >= 0; i--)
        {
            CharacterDeathRecord record = characterDeaths[i];
            if (record == null || record.characterCard == null)
            {
                continue;
            }

            if (currentTurnNumber - record.turnNumber > safeLookbackTurns)
            {
                continue;
            }

            if (seenCards.Add(record.characterCard))
            {
                choices.Add(record.characterCard);
            }
        }

        return choices;
    }

    private static int ResolveCurrentTurnNumber()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null ? Mathf.Max(1, gameManager.turnNumber) : 1;
    }
}
