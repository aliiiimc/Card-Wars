// Data model for world effect cards (structures, hazards, weather) with optional health, damage, revenue, vision, and movement modifiers.
using UnityEngine;

[CreateAssetMenu(fileName = "WorldEffectCard", menuName = "Cards/World Effect")]
public class WorldEffectCardData : CardData
{
    [Header("World Effect")]
    public Sprite manifestedSprite;

    public WorldEffectCategory category;

    public OptionalInt structureHp;

    public OptionalInt structureDamage;

    public OptionalInt revenuePerTurn;

    public OptionalInt visionModifier;

    public OptionalInt movementModifier;

    // Optional duration in turns. Use 0 for permanent/persistent effects.
    public int durationTurns;

    public OptionalInt worldEffectMovementCapacity;

    public override OptionalInt MovementCapacity => worldEffectMovementCapacity;
}