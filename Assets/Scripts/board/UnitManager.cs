using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public float smoothMoveDuration = 0.25f;
    public float walkBobHeight = 0.08f;
    public float walkLeanAngle = 4f;
    public float walkSquashAmount = 0.08f;
    public Color moveHighlightColor = new Color(0.42f, 0.93f, 0.68f);
    public Color attackHighlightColor = new Color(1f, 0.70f, 0.30f);

    private Unit selectedUnit;
    private List<HexTile> moveTiles = new List<HexTile>();
    private List<HexTile> attackTiles = new List<HexTile>();
    private HexGrid grid;
    private WorldEffectManager worldEffectManager;
    private readonly List<ISpecialCardScript> specialCardScripts = new List<ISpecialCardScript>
    {
        new Archer(),
        new EuropeanKing(),
        new Miner(),
        new UfoCow()
    };

    private GameManager gameManager; //Ali
    private string lastActiveOwner = "";
    private bool isAnimatingUnit;


    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>(); //Def de GameManager
        grid = FindFirstObjectByType<HexGrid>();
        worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        ResetUnitsForActiveOwnerIfNeeded();
    }


    void Update()
    {
        ResetUnitsForActiveOwnerIfNeeded();

        if (isAnimatingUnit)
        {
            return;
        }

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
                else if (moveTiles.Contains(clickedTile) && IsValidMoveDestination(clickedTile))
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

        // Movement range (green). The range uses the unit's remaining turn budget, not the full card range again.
        if (selectedUnit.CanMove())
        {
            moveTiles = HexUtils.GetReachableMoveTiles(tile, selectedUnit.GetRemainingMovement(), grid);
            AppendReachableEnemyMineTiles(tile, selectedUnit, moveTiles);
            moveTiles.RemoveAll(t => !IsInsideTurnStartRange(selectedUnit, t));
            foreach (HexTile t in moveTiles)
            {
                t.Highlight(moveHighlightColor);
            }
        }

        // Attack range (red) — highlight enemies within attackRange
        if (selectedUnit.CanAttack())
        {
            int effectiveAttackRange = GetAttackRangeForUnit(selectedUnit);
            if (effectiveAttackRange > selectedUnit.attackRange)
            {
                Debug.Log($"[SpecialTrigger][Archer] Extended attack range from {selectedUnit.attackRange} to {effectiveAttackRange}.");
            }
            attackTiles = HexUtils.GetTilesInRange(tile, effectiveAttackRange, grid);
            foreach (HexTile t in attackTiles)
            {
                if (IsEnemyTarget(t))
                    t.Highlight(attackHighlightColor);
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

        Unit movingUnit = selectedUnit;
        CharacterCardData unitCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(movingUnit, out unitCardData);
        int movementCost = HexUtils.GetMoveDistance(
            movingUnit.currentTile,
            targetTile,
            grid,
            movingUnit.GetRemainingMovement());

        if (movementCost <= 0)
        {
            DeselectUnit();
            return;
        }

        if (!IsInsideTurnStartRange(movingUnit, targetTile))
        {
            DeselectUnit();
            return;
        }

        Vector3 startPosition = movingUnit.transform.position;
        Vector3 targetPosition = targetTile.transform.position;
        bool steppedOnEnemyMine = IsEnemyMineTileForUnit(targetTile, movingUnit);
        int mineDamage = steppedOnEnemyMine ? Mathf.Max(1, targetTile.mineDamage) : 0;

        if (steppedOnEnemyMine)
        {
            if (worldEffectManager == null)
            {
                worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
            }

            if (worldEffectManager != null)
            {
                worldEffectManager.Remove(targetTile);
            }
            else
            {
                targetTile.RemoveUnit();
            }
        }

        specialScript?.OnBeforeMove(movingUnit, unitCardData);
        movingUnit.currentTile.RemoveUnit();
        movingUnit.PlaceOnTile(targetTile, snapToTile: false);
        bool consumeMoveAction = specialScript == null || specialScript.ConsumeMoveAction(movingUnit, unitCardData);
        if (consumeMoveAction)
        {
            movingUnit.MarkMoved(movementCost);
        }
        else
        {
            Debug.Log($"[SpecialTrigger][Miner] Move action preserved after moving to ({targetTile.coord.q},{targetTile.coord.r}).");
        }

        if (steppedOnEnemyMine)
        {
            movingUnit.health -= mineDamage;
            Debug.Log($"[SpecialTrigger][Mines] Mine triggered at ({targetTile.coord.q},{targetTile.coord.r}). {movingUnit.name} took {mineDamage} damage. HP now {movingUnit.health}.");
            if (movingUnit.health <= 0)
            {
                movingUnit.Die();
                DeselectUnit();
                return;
            }
        }

        DeselectUnit();

        StartCoroutine(MoveUnitSmoothly(movingUnit, startPosition, targetPosition, specialScript, unitCardData, targetTile));
    }

    void AttackTarget(HexTile targetTile)
    {
        if (selectedUnit == null || !selectedUnit.CanAttack())
        {
            DeselectUnit();
            return;
        }

        CharacterCardData attackerCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(selectedUnit, out attackerCardData);
        bool isSpecialTarget = specialScript != null && specialScript.CanTarget(selectedUnit, attackerCardData, targetTile, GetActiveOwner());
        if (isSpecialTarget && specialScript.TryHandleAttack(selectedUnit, attackerCardData, targetTile, GetActiveOwner()))
        {
            selectedUnit.MarkAttacked();
            DeselectUnit();
            return;
        }

        Unit target = FindUnitOnTile(targetTile);
        if (target != null)
        {
            target.ApplyDamage(selectedUnit.attack);
            Debug.Log($"Attacked! Target health: {target.health}");

            if (target.health <= 0)
            {
                Debug.Log("Target died!");
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
        // Ali: colonization rule - special units can convert enemy world effects instead of dealing damage.
        else if (targetTile.tileType == "worldEffect"
                 && selectedUnit.canColonizeEnemyWorldEffects
                 && targetTile.owner != GetActiveOwner())
        {
            if (worldEffectManager == null)
            {
                worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
            }

            if (worldEffectManager != null && worldEffectManager.TryColonize(targetTile, GetActiveOwner()))
            {
                Debug.Log("Colonized enemy world effect.");
            }
            else
            {
                Debug.LogWarning("Colonization failed: world effect manager rejected this target.");
                DeselectUnit();
                return;
            }
        }
        else if (isSpecialTarget)
        {
            Debug.LogWarning("Special target was selected but the special card did not handle this attack.");
            DeselectUnit();
            return;
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
        if (tile == null || selectedUnit == null)
        {
            return false;
        }

        CharacterCardData attackerCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(selectedUnit, out attackerCardData);
        bool canSpecialTarget = specialScript != null
            && specialScript.CanTarget(selectedUnit, attackerCardData, tile, GetActiveOwner());

        // Ali: special units can target enemy world effects for colonization, while normal units keep classic unit/fort targeting.
        bool canTargetEnemyWorldEffect = selectedUnit.canColonizeEnemyWorldEffects
            && tile.tileType == "worldEffect";

        return tile.owner != "none"
            && tile.owner != GetActiveOwner()
            && (tile.tileType == "unit" || tile.tileType == "fort" || canTargetEnemyWorldEffect || canSpecialTarget);
    }

    bool IsValidMoveDestination(HexTile tile)
    {
        if (selectedUnit == null || tile == null)
        {
            return false;
        }

        return tile.IsEmpty() || IsEnemyMineTileForUnit(tile, selectedUnit);
    }

    bool IsEnemyMineTileForUnit(HexTile tile, Unit unit)
    {
        return tile != null
            && unit != null
            && tile.tileType == "worldEffect"
            && tile.isMineTile
            && tile.owner != "none"
            && tile.owner != unit.owner;
    }

    void AppendReachableEnemyMineTiles(HexTile startTile, Unit unit, List<HexTile> destinationTiles)
    {
        if (startTile == null || unit == null || destinationTiles == null || grid == null)
        {
            return;
        }

        List<HexTile> inRangeTiles = HexUtils.GetTilesInRange(startTile, unit.GetRemainingMovement(), grid);
        for (int i = 0; i < inRangeTiles.Count; i++)
        {
            HexTile tile = inRangeTiles[i];
            if (!IsEnemyMineTileForUnit(tile, unit))
            {
                continue;
            }

            int distance = HexUtils.GetMoveDistance(startTile, tile, grid, unit.GetRemainingMovement());
            if (distance <= 0)
            {
                continue;
            }

            if (!destinationTiles.Contains(tile))
            {
                destinationTiles.Add(tile);
            }
        }
    }

    bool IsInsideTurnStartRange(Unit unit, HexTile tile)
    {
        if (unit == null || tile == null || unit.turnStartTile == null)
        {
            return true;
        }

        return HexUtils.GetHexDistance(unit.turnStartTile, tile) <= unit.moveRange;
    }

    int GetAttackRangeForUnit(Unit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        CharacterCardData unitCardData;
        ISpecialCardScript specialScript = ResolveSpecialScript(unit, out unitCardData);
        if (specialScript != null)
        {
            return Mathf.Max(0, specialScript.GetAttackRange(unit, unitCardData));
        }

        return Mathf.Max(0, unit.attackRange);
    }

    ISpecialCardScript ResolveSpecialScript(Unit unit, out CharacterCardData unitCardData)
    {
        unitCardData = unit != null ? unit.sourceCharacterCardData : null;
        if (unit == null || unitCardData == null)
        {
            return null;
        }

        for (int i = 0; i < specialCardScripts.Count; i++)
        {
            ISpecialCardScript script = specialCardScripts[i];
            if (script != null && script.IsMatch(unit, unitCardData))
            {
                return script;
            }
        }

        return null;
    }

    IEnumerator MoveUnitSmoothly(Unit unit, Vector3 startPosition, Vector3 targetPosition, ISpecialCardScript specialScript, CharacterCardData unitCardData, HexTile destinationTile)
    {
        isAnimatingUnit = true;

        Vector3 originalScale = unit != null ? unit.transform.localScale : Vector3.one;
        Quaternion originalRotation = unit != null ? unit.transform.rotation : Quaternion.identity;

        if (smoothMoveDuration <= 0f)
        {
            if (unit != null)
            {
                unit.transform.position = targetPosition;
                unit.transform.localScale = originalScale;
                unit.transform.rotation = originalRotation;
            }

            specialScript?.OnAfterMove(unit, unitCardData, destinationTile);
            isAnimatingUnit = false;
            yield break;
        }

        float moveDirection = Mathf.Sign(targetPosition.x - startPosition.x);
        if (Mathf.Approximately(moveDirection, 0f))
        {
            moveDirection = 1f;
        }

        float elapsed = 0f;
        while (unit != null && elapsed < smoothMoveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / smoothMoveDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float walkCycle = Mathf.Sin(progress * Mathf.PI * 4f);
            float bobOffset = Mathf.Abs(walkCycle) * walkBobHeight;
            float leanOffset = walkCycle * walkLeanAngle * -moveDirection;
            float squash = Mathf.Abs(walkCycle) * walkSquashAmount;

            unit.transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress) + Vector3.up * bobOffset;
            unit.transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, leanOffset);
            unit.transform.localScale = new Vector3(
                originalScale.x * (1f + squash * 0.5f),
                originalScale.y * (1f - squash),
                originalScale.z
            );
            yield return null;
        }

        if (unit != null)
        {
            unit.transform.position = targetPosition;
            unit.transform.localScale = originalScale;
            unit.transform.rotation = originalRotation;
        }

        specialScript?.OnAfterMove(unit, unitCardData, destinationTile);
        isAnimatingUnit = false;
    }
}
