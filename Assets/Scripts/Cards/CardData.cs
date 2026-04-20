// Base abstract class defining the core data model shared by all card types (Character, Spell, WorldEffect).
// Provides common properties like name, cost, and sprite, plus polymorphic movement capacity.
using UnityEngine;
using UnityEngine.Serialization;

public abstract class CardData : ScriptableObject
{
    [Header("Core")]
    public string cardName;

    public int cost;

    [FormerlySerializedAs("artwork")]
    public Sprite handDeckSprite;


    // public CardRarity rarity = CardRarity.Common;

    [TextArea]
    public string description;

    public abstract OptionalInt MovementCapacity { get; }
    public string DisplayName => string.IsNullOrWhiteSpace(cardName) ? name : cardName;

    public virtual CardRuntimeState CreateRuntimeState()
    {
        return new CardRuntimeState(this);
    }
}
