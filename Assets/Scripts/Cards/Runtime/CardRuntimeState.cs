using System;
using UnityEngine;

[Serializable]
public class CardRuntimeState
{
    // Immutable ScriptableObject definition that this runtime state came from.
    [SerializeField] private CardData sourceCard;

    // Current location of the card in match flow.
    [SerializeField] private CardZone currentZone;

    // True when represented by an in-scene object on the board.
    [SerializeField] private bool isManifestedOnBoard;

    // Axial hex coordinate used when manifested.
    [SerializeField] private AxialCoord boardPosition;

    // Current movement points for this turn/state.
    [SerializeField] private int currentMovementCapacity;

    // Optional mutable stats depending on card type.
    [SerializeField] private OptionalInt currentHp;
    [SerializeField] private OptionalInt currentDamage;
    [SerializeField] private OptionalInt currentRevenue;

    // Attack readiness mainly used by character cards.
    [SerializeField] private bool isReadyToAttack;

    public CardData SourceCard => sourceCard;
    public CardZone CurrentZone => currentZone;
    public bool IsManifestedOnBoard => isManifestedOnBoard;
    public AxialCoord BoardPosition => boardPosition;
    public int CurrentMovementCapacity => currentMovementCapacity;
    public OptionalInt CurrentHp => currentHp;
    public OptionalInt CurrentDamage => currentDamage;
    public OptionalInt CurrentRevenue => currentRevenue;
    public bool IsReadyToAttack => isReadyToAttack;

    // Builds a mutable state object from static card definition data.
    public CardRuntimeState(CardData sourceCard)
    {
        if (sourceCard == null)
        {
            throw new ArgumentNullException(nameof(sourceCard));
        }

        this.sourceCard = sourceCard;
        currentMovementCapacity = sourceCard.MovementCapacity;
        currentHp = OptionalInt.None;
        currentDamage = OptionalInt.None;
        currentRevenue = OptionalInt.None;

        InitializeCardSpecificState();
    }

    // Copies type-specific initial values from the concrete card class.
    private void InitializeCardSpecificState()
    {
        if (sourceCard is CharacterCardData characterCard)
        {
            currentHp = new OptionalInt(characterCard.maxHp);
            currentDamage = new OptionalInt(characterCard.attackDamage);
            isReadyToAttack = characterCard.startsReadyToAttack;
            return;
        }

        if (sourceCard is WorldEffectCardData worldEffectCard)
        {
            currentHp = worldEffectCard.structureHp;
            currentDamage = worldEffectCard.structureDamage;
            currentRevenue = worldEffectCard.revenuePerTurn;
        }
    }

    // Moves card to a zone and clears board-only fields when leaving board.
    public void MoveToZone(CardZone zone)
    {
        currentZone = zone;

        if (zone != CardZone.Board)
        {
            isManifestedOnBoard = false;
            boardPosition = default;
        }
    }

    // Places card onto board and records the board coordinate.
    public void ManifestOnBoard(AxialCoord position)
    {
        currentZone = CardZone.Board;
        isManifestedOnBoard = true;
        boardPosition = position;
    }

    // Consumes movement points safely without allowing negative values.
    public void ConsumeMovement(int movementCost)
    {
        currentMovementCapacity = Mathf.Max(0, currentMovementCapacity - Mathf.Max(0, movementCost));
    }

    // Resets movement back to definition value, usually at start of turn.
    public void ResetMovementFromDefinition()
    {
        currentMovementCapacity = sourceCard == null ? 0 : sourceCard.MovementCapacity;
    }

    // Updates attack readiness flag after combat or turn transitions.
    public void SetAttackReady(bool ready)
    {
        isReadyToAttack = ready;
    }
}
