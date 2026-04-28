using UnityEngine;

public sealed class GameManagerCardStateWriter : MonoBehaviour, ICardStateWriter
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool autoResolveGameManager = true;
    [SerializeField] private bool logTransactions = true;
    [SerializeField] private string player1Key = "player";
    [SerializeField] private string player2Key = "enemy";
    [SerializeField] private HexGrid boardSource;//Ali : référence vers le board
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

        card.ManifestOnBoard(tile);

        //Ali:

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }

        HexTile targetTile = boardSource != null ? boardSource.GetTile(tile) : null;
        string owner = LastActingPlayerId == PlayerKeyResolver.PlayerTwoKey //décide à qui appartient l’unité créée
            ? PlayerKeyResolver.PlayerTwoKey
            : PlayerKeyResolver.PlayerOneKey;

        if (card.SourceCard is CharacterCardData && targetTile != null)
        {
            boardSource.SpawnUnitFromCard(targetTile, owner, card);
        }


        LogTransaction($"ManifestCard: {card.SourceCard.DisplayName} at {tile}.");
    }

    public void ApplyDamage(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        card.ApplyDamage(amount);
        LogTransaction($"ApplyDamage: {card.SourceCard.DisplayName} amount={Mathf.Max(0, amount)}.");
    }

    public void ApplyHeal(CardRuntimeState card, int amount)
    {
        if (card == null)
        {
            return;
        }

        card.ApplyHeal(amount);
        LogTransaction($"ApplyHeal: {card.SourceCard.DisplayName} amount={Mathf.Max(0, amount)}.");
    }

    public void ModifyDamage(CardRuntimeState card, int delta)
    {
        if (card == null)
        {
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

    private void LogTransaction(string message)
    {
        if (logTransactions)
        {
            Debug.Log($"[GameManagerCardStateWriter] {message}");
        }
    }
}
