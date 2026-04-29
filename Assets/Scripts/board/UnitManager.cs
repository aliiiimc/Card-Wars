using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    private Unit selectedUnit;
    private List<HexTile> moveTiles = new List<HexTile>();
    private List<HexTile> attackTiles = new List<HexTile>();
    private HexGrid grid;

    private GameManager gameManager; //Ali
    private string lastActiveOwner = "";


    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>(); //Def de GameManager
        grid = FindFirstObjectByType<HexGrid>();
        ResetUnitsForActiveOwnerIfNeeded();
    }


    void Update()
    {
        ResetUnitsForActiveOwnerIfNeeded();

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider == null) return;

            HexTile clickedTile = hit.collider.GetComponent<HexTile>();
            if (clickedTile == null) return;

            if (selectedUnit == null)
            {
                // Select one of the current player's units.
                if (clickedTile.tileType == "unit" && clickedTile.owner == GetActiveOwner())
                {
                    SelectUnit(clickedTile);
                }
            }
            else
            {
                // Attack an enemy in attack range
                if (attackTiles.Contains(clickedTile) && IsEnemyTarget(clickedTile))
                {
                    AttackTarget(clickedTile);
                }
                // Move to an empty tile in move range
                else if (moveTiles.Contains(clickedTile) && clickedTile.IsEmpty())
                {
                    MoveUnit(clickedTile);
                }
                else
                {
                    DeselectUnit();
                }
            }
        }
    }

    void SelectUnit(HexTile tile)
    {
        selectedUnit = FindUnitOnTile(tile);
        if (selectedUnit == null) return;

        // Movement range (green)
        if (selectedUnit.CanMove())
        {
            moveTiles = HexUtils.GetTilesInRange(tile, selectedUnit.moveRange, grid);
            foreach (HexTile t in moveTiles)
            {
                if (t.IsEmpty())
                    t.Highlight(Color.green);
            }
        }

        // Attack range (red) — highlight enemies within attackRange
        if (selectedUnit.CanAttack())
        {
            attackTiles = HexUtils.GetTilesInRange(tile, selectedUnit.attackRange, grid);
            foreach (HexTile t in attackTiles)
            {
                if (IsEnemyTarget(t))
                    t.Highlight(Color.red);
            }
        }
    }

    void MoveUnit(HexTile targetTile)
    {
        if (selectedUnit == null || !selectedUnit.CanMove())
        {
            DeselectUnit();
            return;
        }

        selectedUnit.currentTile.RemoveUnit();
        selectedUnit.PlaceOnTile(targetTile);
        selectedUnit.transform.position = targetTile.transform.position;
        selectedUnit.MarkMoved();
        DeselectUnit();
    }

    void AttackTarget(HexTile targetTile)
    {
        if (selectedUnit == null || !selectedUnit.CanAttack())
        {
            DeselectUnit();
            return;
        }

        Unit target = FindUnitOnTile(targetTile);
        if (target != null)
        {
            target.health -= selectedUnit.attack;
            Debug.Log($"Attacked! Target health: {target.health}");

            if (target.health <= 0)
            {
                Debug.Log("Target died!");
                target.Die();
            }
        }
        else if (targetTile.tileType == "fort") //Ali : Update de AttackTarget
        {
            Debug.Log("Attacked fort!");

            if (gameManager == null)
            {
                Debug.LogWarning("GameManager not found. Cannot damage fort.");
                return;
            }

            if (targetTile.owner == "enemy")
            {
                gameManager.DamagePlayer2Fort(selectedUnit.attack);
            }
            else if (targetTile.owner == "player")
            {
                gameManager.DamagePlayer1Fort(selectedUnit.attack);
            }
        }


        selectedUnit.MarkAttacked();
        DeselectUnit();
    }

    void DeselectUnit()
    {
        foreach (HexTile t in moveTiles) t.ResetColor();
        foreach (HexTile t in attackTiles) t.ResetColor();
        moveTiles.Clear();
        attackTiles.Clear();
        selectedUnit = null;
    }

    Unit FindUnitOnTile(HexTile tile)
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit u in allUnits)
        {
            if (u.currentTile == tile)
                return u;
        }
        return null;
    }

    void ResetUnitsForActiveOwnerIfNeeded()
    {
        string activeOwner = GetActiveOwner();
        if (string.IsNullOrEmpty(activeOwner) || activeOwner == lastActiveOwner)
        {
            return;
        }

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in allUnits)
        {
            if (unit != null && unit.owner == activeOwner)
            {
                unit.ResetTurnActions();
            }
        }

        lastActiveOwner = activeOwner;
    }

    string GetActiveOwner()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || gameManager.currentPlayer == null)
        {
            return "player";
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
        {
            return "enemy";
        }

        return "player";
    }

    bool IsEnemyTarget(HexTile tile)
    {
        return tile != null
            && tile.owner != "none"
            && tile.owner != GetActiveOwner()
            && (tile.tileType == "unit" || tile.tileType == "fort");
    }
}
