using UnityEngine;
using UnityEngine.Serialization;

public abstract class CardData : ScriptableObject
{
    [Header("Core")]
    public string cardName;

    public int cost;

    [FormerlySerializedAs("artwork")]
    public Sprite handDeckSprite;
    [TextArea]
    public string description;

    [Header("Pipeline Mapping")]
    public string validatorId = "target.rules.reusable";
    public string effectId;
    public string specialCardId;

    public abstract OptionalInt MovementCapacity { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;

    public bool MatchesSpecialCard(string expectedSpecialId, string fallbackDisplayName = null)
    {
        if (!string.IsNullOrWhiteSpace(expectedSpecialId) && !string.IsNullOrWhiteSpace(specialCardId))
        {
            return string.Equals(specialCardId.Trim(), expectedSpecialId.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(fallbackDisplayName))
        {
            return false;
        }

        return string.Equals(DisplayName.Trim(), fallbackDisplayName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    public virtual CardRuntimeState CreateRuntimeState()
    {
        return new CardRuntimeState(this);
    }
}
