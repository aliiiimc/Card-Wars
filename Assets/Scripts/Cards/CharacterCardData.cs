// Data model for character/unit cards with health, attack damage, readiness state, and board movement capacity.
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCard", menuName = "Cards/Character")]
public class CharacterCardData : CardData
{
    [Header("Character")]
    public Sprite manifestedSprite;

    public int maxHp;

    public int attackDamage;

    public bool startsReadyToAttack;

    public OptionalInt unitMovementCapacity;

    public override OptionalInt MovementCapacity => unitMovementCapacity;
}
