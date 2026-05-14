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
        string owner = ResolveOwnerForManifestedCard();


        // Ali: keep board manifestation inside the writer so Character and World Effect cards follow the same pipeline path.
        // Ali: the writer is the runtime layer that applies the real board result of a card play, so Character and World Effect manifestation should both happen here.
        if (targetTile != null)
        {
            if (card.SourceCard is CharacterCardData)
            {
                boardSource.SpawnUnitFromCard(targetTile, owner, card);
            }
            else if (card.SourceCard is WorldEffectCardData worldEffectCard)
            {
                targetTile.PlaceWorldEffect(owner, worldEffectCard.manifestedSprite);
            }
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

    private void LogTransaction(string message)
    {
        if (logTransactions)
        {
            Debug.Log($"[GameManagerCardStateWriter] {message}");
        }
    }
}
