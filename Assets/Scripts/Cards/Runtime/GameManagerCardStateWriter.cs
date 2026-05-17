using UnityEngine;
using System.Collections.Generic;

public sealed class GameManagerCardStateWriter : MonoBehaviour, ICardStateWriter
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool autoResolveGameManager = true;
    [SerializeField] private bool logTransactions = true;
    [SerializeField] private string player1Key = "player";
    [SerializeField] private string player2Key = "enemy";
    [SerializeField] private HexGrid boardSource;//Ali : référence vers le board
    [SerializeField] private WorldEffectManager worldEffectManager;
    // [SerializeField] : variable privée dans le code, mais visible dans l’Inspector Unity.


    public string LastActingPlayerId { get; private set; }

    private void Awake()
    {
        if (autoResolveGameManager && gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        //Ali: Load the hexgrid variable
        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

    }

    public bool TrySpendCost(string playerId, int amount)
    {
        LastActingPlayerId = playerId;

        PlayerState playerState = ResolvePlayer(playerId);
        if (playerState == null)
        {
            LogTransaction($"TrySpendCost failed: unknown player '{playerId}'.");
            return false;
        }

        int cost = Mathf.Max(0, amount);
        if (playerState.money < cost)
        {
            LogTransaction($"TrySpendCost failed: player '{playerId}' has {playerState.money}, needs {cost}.");
            return false;
        }

        playerState.money -= cost;
        LogTransaction($"TrySpendCost success: player '{playerId}' paid {cost}. Remaining={playerState.money}.");
        return true;
    }

    public void AddRevenue(string playerId, int amount)
    {
        LastActingPlayerId = playerId;

        PlayerState playerState = ResolvePlayer(playerId);
        if (playerState == null)
        {
            LogTransaction($"AddRevenue failed: unknown player '{playerId}'.");
            return;
        }

        int add = Mathf.Max(0, amount);
        playerState.money += add;
        LogTransaction($"AddRevenue: player='{playerId}' amount={add} total={playerState.money}.");
    }

    public void MoveCardToZone(CardRuntimeState card, CardZone zone)
    {
        if (card == null)
        {
            return;
        }

        card.MoveToZone(zone);
        LogTransaction($"MoveCardToZone: {card.SourceCard.DisplayName} -> {zone}.");
    }

    public void ManifestCard(CardRuntimeState card, AxialCoord tile)
    {
        if (card == null)
        {
            return;
        }

        //Ali:

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

        HexTile targetTile = boardSource != null ? boardSource.GetTile(tile) : null;
        string owner = ResolveOwnerForManifestedCard();
        bool placementSucceeded = false;


        // Ali: keep board manifestation inside the writer so Character and World Effect cards follow the same pipeline path.
        // Ali: the writer is the runtime layer that applies the real board result of a card play, so Character and World Effect manifestation should both happen here.
        if (targetTile != null)
        {
            if (card.SourceCard is CharacterCardData)
            {
                placementSucceeded = boardSource != null
                    && boardSource.SpawnUnitFromCard(targetTile, owner, card) != null;
                if (!placementSucceeded)
                {
                    LogTransaction(
                        $"ManifestCard character placement failed: owner='{owner}', tileType='{targetTile.tileType}', tileOwner='{targetTile.owner}'.");
                }
                else if (boardSource != null
                    && !boardSource.IsInPlayerDeploymentZone(tile, owner)
                    && BoardPlacementRules.CanPlaceCharacter(tile, owner, boardSource))
                {
                    LogTransaction($"[SpecialTrigger][Camp] Character spawned using Camp override at {tile}.");
                }
            }
            else if (card.SourceCard is WorldEffectCardData worldEffectCard)
            {
                if (worldEffectManager == null)
                {
                    LogTransaction("ManifestCard failed: no WorldEffectManager found.");
                }
                else
                {
                    placementSucceeded = targetTile.IsEmpty()
                        ? worldEffectManager.TryPlaceFromCard(targetTile, owner, card, out _)
                        : targetTile.tileType == "worldEffect"
                            && worldEffectManager.TryReplace(targetTile, owner, card, out _);

                    if (!placementSucceeded)
                    {
                        LogTransaction(
                            $"ManifestCard world effect placement failed: owner='{owner}', tileType='{targetTile.tileType}', tileOwner='{targetTile.owner}'.");
                    }
                }

                if (placementSucceeded)
                {
                    ApplySpecialWorldEffectOnManifest(worldEffectCard, card, owner, targetTile);
                }
            }
        }
        else
        {
            LogTransaction($"ManifestCard failed: target tile '{tile}' was not found.");
        }

        if (placementSucceeded)
        {
            card.ManifestOnBoard(tile);
            LogTransaction($"ManifestCard: {card.SourceCard.DisplayName} at {tile}.");
            return;
        }

        LogTransaction($"ManifestCard aborted: {card.SourceCard.DisplayName} was not manifested on board.");
    }

    public void ApplyDamage(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ApplyDamage(safeAmount);
            LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} realUnit amount={safeAmount}.");
            return;
        }

        card.ApplyDamage(safeAmount);
        LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} amount={safeAmount}.");
    }

    public void ApplyHeal(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ApplyHeal(safeAmount);
            LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} realUnit amount={safeAmount}.");
            return;
        }

        card.ApplyHeal(safeAmount);
        LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} amount={safeAmount}.");
    }

    //Ali:
    public void ApplyFortDamage(string playerId, int amount)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);// On prend la plus grande valeur entre 0 et amount, On veut empêcher des valeurs négatives de passer.

        if (safeAmount <= 0)
        {
            return;
        }

        if (playerId == player1Key || (gameManager.player1 != null && playerId == gameManager.player1.playerName))
        {
            gameManager.DamagePlayer1Fort(safeAmount);
            LogTransaction($"ApplyFortDamage: player='{playerId}' amount={safeAmount}.");
            return;
        }

        if (playerId == player2Key || (gameManager.player2 != null && playerId == gameManager.player2.playerName))
        {
            gameManager.DamagePlayer2Fort(safeAmount);
            LogTransaction($"ApplyFortDamage: player='{playerId}' amount={safeAmount}.");
        }
    }

    //Ali:
    public void ApplyFortHeal(string playerId, int amount)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        if (playerId == player1Key || (gameManager.player1 != null && playerId == gameManager.player1.playerName))
        {
            gameManager.HealPlayer1Fort(safeAmount);
            LogTransaction($"ApplyFortHeal: player='{playerId}' amount={safeAmount}.");
            return;
        }

        if (playerId == player2Key || (gameManager.player2 != null && playerId == gameManager.player2.playerName))
        {
            gameManager.HealPlayer2Fort(safeAmount);
            LogTransaction($"ApplyFortHeal: player='{playerId}' amount={safeAmount}.");
        }
    }


    public void ModifyDamage(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ModifyAttack(delta);
            LogTransaction($"ModifyDamage: {card.SourceCard.DisplayName} realUnit delta={delta}.");
            return;
        }

        card.ModifyDamage(delta);
        LogTransaction($"ModifyDamage: {card.SourceCard.DisplayName} delta={delta}.");
    }

    public void ModifyMovement(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
            return;
        }

        Unit boardUnit = FindUnitForCard(card);
        if (boardUnit != null)
        {
            boardUnit.ModifyMovementRange(delta);
            LogTransaction($"ModifyMovement: {card.SourceCard.DisplayName} realUnit delta={delta}.");
            return;
        }

        card.ModifyMovement(delta);
        LogTransaction($"ModifyMovement: {card.SourceCard.DisplayName} delta={delta}.");
    }

    public int GetMoney(string playerId)
    {
        PlayerState playerState = ResolvePlayer(playerId);
        return playerState != null ? playerState.money : 0;
    }

    private PlayerState ResolvePlayer(string playerId)
    {
        if (gameManager == null || string.IsNullOrWhiteSpace(playerId))
        {
            return null;
        }

        if (gameManager.player1 != null && (playerId == player1Key || playerId == gameManager.player1.playerName))
        {
            return gameManager.player1;
        }

        if (gameManager.player2 != null && (playerId == player2Key || playerId == gameManager.player2.playerName))
        {
            return gameManager.player2;
        }

        if (gameManager.currentPlayer != null && playerId == "current")
        {
            return gameManager.currentPlayer;
        }

        return null;
    }

    private string ResolveOwnerForManifestedCard()
    {
        if (LastActingPlayerId == PlayerKeyResolver.PlayerTwoKey || LastActingPlayerId == player2Key)
        {
            return PlayerKeyResolver.PlayerTwoKey;
        }

        if (LastActingPlayerId == PlayerKeyResolver.PlayerOneKey || LastActingPlayerId == player1Key)
        {
            return PlayerKeyResolver.PlayerOneKey;
        }

        if (gameManager != null && gameManager.currentPlayer != null)
        {
            if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
            {
                return PlayerKeyResolver.PlayerTwoKey;
            }

            if (ReferenceEquals(gameManager.currentPlayer, gameManager.player1))
            {
                return PlayerKeyResolver.PlayerOneKey;
            }
        }

        return PlayerKeyResolver.PlayerOneKey;
    }

    private Unit FindUnitForCard(CardRuntimeState card)
    {
        if (card == null)
        {
            return null;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit != null && ReferenceEquals(unit.RuntimeCard, card))
            {
                return unit;
            }
        }

        return null;
    }

    private void LogTransaction(string message)
    {
        if (logTransactions)
        {
            Debug.Log($"[GameManagerCardStateWriter] {message}");
        }
    }

    private void ApplySpecialWorldEffectOnManifest(WorldEffectCardData worldEffectCard, CardRuntimeState runtimeCard, string owner, HexTile targetTile)
    {
        if (worldEffectCard == null || runtimeCard == null || targetTile == null)
        {
            return;
        }

        if (worldEffectManager == null)
        {
            worldEffectManager = FindFirstObjectByType<WorldEffectManager>();
        }
        if (worldEffectManager == null)
        {
            worldEffectManager = CreateWorldEffectManagerFallback();
        }

        string cardName = worldEffectCard.DisplayName != null
            ? worldEffectCard.DisplayName.Trim().ToLowerInvariant()
            : string.Empty;

        // Reset card-specific tile metadata before assigning the new world-effect behavior.
        if (worldEffectManager != null)
        {
            worldEffectManager.TryClearSpecialData(targetTile);
        }

        if (cardName == "wheat field")
        {
            WheatField wheatField = new WheatField();
            if (!wheatField.ApplyFieldCluster(
                boardSource,
                targetTile,
                owner,
                worldEffectCard.manifestedSprite,
                runtimeCard,
                out string clusterId))
            {
                LogTransaction("Wheat field cluster creation failed.");
            }
            else
            {
                LogTransaction($"Wheat field cluster created: {clusterId}.");
            }

            return;
        }

        if (cardName == "mines")
        {
            Mines mines = new Mines();
            MinesCardData minesCardData = worldEffectCard as MinesCardData;
            int placedMineCount = PlaceMinesRandomly(minesCardData, runtimeCard, owner, targetTile, mines);
            LogTransaction($"{mines.GetEnemyWarningMessage()} Placed {placedMineCount} mine(s).");
            return;
        }

        if (cardName == "camp")
        {
            Camp camp = new Camp();
            if (camp.ForcesNewSpawnLocation(worldEffectCard as CampCardData))
            {
                if (worldEffectManager != null && worldEffectManager.TrySetCampData(targetTile))
                {
                    LogTransaction("Camp world effect activated on tile.");
                }
                else
                {
                    LogTransaction("Camp world effect activation failed.");
                }
            }
        }
    }

    private int PlaceMinesRandomly(MinesCardData worldEffectCard, CardRuntimeState runtimeCard, string owner, HexTile targetTile, Mines mines)
    {
        if (worldEffectManager == null || boardSource == null || worldEffectCard == null || runtimeCard == null || mines == null)
        {
            return 0;
        }

        // Remove the initial anchor manifestation: mines are distributed randomly in owner's half.
        if (targetTile != null && targetTile.tileType == "worldEffect")
        {
            worldEffectManager.Remove(targetTile);
        }

        List<HexTile> candidates = GetRandomMineCandidates(owner);
        int requestedCount = Mathf.Max(1, mines.GetMinesToPlace(worldEffectCard));
        int mineDamage = Mathf.Max(1, mines.GetMineDamage(worldEffectCard));
        int placedCount = 0;

        for (int i = 0; i < candidates.Count && placedCount < requestedCount; i++)
        {
            HexTile candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (!worldEffectManager.TryPlaceFromCard(candidate, owner, runtimeCard, out _))
            {
                continue;
            }

            worldEffectManager.TrySetMineData(candidate, mineDamage);
            placedCount++;
        }

        return placedCount;
    }

    private List<HexTile> GetRandomMineCandidates(string owner)
    {
        List<HexTile> candidates = new List<HexTile>();
        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        if (allTiles == null || allTiles.Length == 0 || boardSource == null)
        {
            return candidates;
        }

        for (int i = 0; i < allTiles.Length; i++)
        {
            HexTile tile = allTiles[i];
            if (tile == null || !tile.IsEmpty())
            {
                continue;
            }

            if (!boardSource.IsInPlayerHalf(tile.coord, owner))
            {
                continue;
            }

            candidates.Add(tile);
        }

        // Fisher-Yates shuffle
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            HexTile temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        return candidates;
    }

    private WorldEffectManager CreateWorldEffectManagerFallback()
    {
        GameObject managerObject = new GameObject("WorldEffectManager");
        WorldEffectManager manager = managerObject.AddComponent<WorldEffectManager>();
        Debug.LogWarning("[GameManagerCardStateWriter] WorldEffectManager was missing in scene. Created runtime fallback.");
        return manager;
    }
}
