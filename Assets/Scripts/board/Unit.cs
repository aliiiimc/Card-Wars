using UnityEngine;

public class Unit : MonoBehaviour
{
    public int moveRange = 2;
    public int attackRange = 1;
    public int health = 10;
    public int attack = 3;
    public string owner = "player";
    public HexTile currentTile;
    public HexTile turnStartTile;
    public bool hasMovedThisTurn;
    public bool hasAttackedThisTurn;
    public int movementSpentThisTurn;

    private CardRuntimeState runtimeCard;

    // Ali: runtime flag copied from card data for world-effect capture rules.
    public bool canColonizeEnemyWorldEffects;


    // Ali: track whether a spawned unit is allowed to attack yet, instead of assuming every new unit is immediately ready.
    public bool isReadyToAttack = true;

    public CardRuntimeState RuntimeCard => runtimeCard;


    public bool CanMove()
    {
        return !hasAttackedThisTurn && GetRemainingMovement() > 0;
    }


    // Ali: a unit can only attack if it has not attacked yet and its summon/readiness rule allows it.
    public bool CanAttack()
    {
        return isReadyToAttack && !hasAttackedThisTurn && !HasExhaustedMovementThisTurn();
    }

    public int GetRemainingMovement()
    {
        return Mathf.Max(0, moveRange - movementSpentThisTurn);
    }

    public bool HasExhaustedMovementThisTurn()
    {
        return hasMovedThisTurn && GetRemainingMovement() <= 0;
    }

    public void MarkMoved(int movementCost)
    {
        int safeCost = Mathf.Max(0, movementCost);
        movementSpentThisTurn = Mathf.Min(moveRange, movementSpentThisTurn + safeCost);
        hasMovedThisTurn = movementSpentThisTurn > 0;
    }

    public void MarkAttacked()
    {
        hasAttackedThisTurn = true;
    }

    public void LinkRuntimeCard(CardRuntimeState card)
    {
        runtimeCard = card;
        SyncStatsFromRuntimeCard();
    }

    public void ApplyDamage(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtimeCard != null)
        {
            runtimeCard.ApplyDamage(safeAmount);
            SyncStatsFromRuntimeCard();
        }
        else
        {
            health = Mathf.Max(0, health - safeAmount);
        }

        if (health <= 0)
        {
            Die();
        }
    }

    public void ApplyHeal(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtimeCard != null)
        {
            runtimeCard.ApplyHeal(safeAmount);
            SyncStatsFromRuntimeCard();
            return;
        }

        health += safeAmount;
    }

    public void ModifyAttack(int delta)
    {
        if (runtimeCard != null)
        {
            runtimeCard.ModifyDamage(delta);
            SyncStatsFromRuntimeCard();
            return;
        }

        attack = Mathf.Max(0, attack + delta);
    }

    public void ModifyMovementRange(int delta)
    {
        if (runtimeCard != null)
        {
            runtimeCard.ModifyMovement(delta);
            SyncStatsFromRuntimeCard();
            return;
        }

        moveRange = Mathf.Max(0, moveRange + delta);
        movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, moveRange);
    }

    private void SyncStatsFromRuntimeCard()
    {
        if (runtimeCard == null)
        {
            return;
        }

        if (runtimeCard.CurrentHp.HasValue)
        {
            health = runtimeCard.CurrentHp.Value;
        }

        if (runtimeCard.CurrentDamage.HasValue)
        {
            attack = runtimeCard.CurrentDamage.Value;
        }

        if (runtimeCard.CurrentMovementCapacity.HasValue)
        {
            moveRange = runtimeCard.CurrentMovementCapacity.Value;
            movementSpentThisTurn = Mathf.Min(movementSpentThisTurn, moveRange);
        }
    }

    public void ResetTurnActions()
    {
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        movementSpentThisTurn = 0;
        turnStartTile = currentTile;
        // A unit that was not ready on summon becomes ready on its owner's next turn.
        isReadyToAttack = true;
    }

    public void PlaceOnTile(HexTile tile, bool snapToTile = true)
    {
        currentTile = tile;
        if (turnStartTile == null)
        {
            turnStartTile = tile;
        }
        tile.tileType = "unit";
        tile.owner = owner;

        if (snapToTile)
        {
            transform.position = tile.transform.position;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (owner == "player")
            sr.color = Color.white;
        else
            sr.color = new Color(1f, 0.55f, 0.58f);
    }

    public void Die()
    {
        runtimeCard?.MoveToZone(CardZone.Discard);

        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            currentTile = null;
        }
        Destroy(gameObject);
    }
}
