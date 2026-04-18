using System.Collections.Generic;

namespace FortGame.Computer
{
    /// <summary>
    /// Immutable view of all data needed by legal action generation.
    /// </summary>
    public sealed class ComputerGameSnapshot
    {
        public GameManager GameManager { get; }
        public HexGrid HexGrid { get; }
        public GamePhase CurrentPhase { get; }
        public int CurrentTurn { get; }

        public PlayerState ActingPlayer { get; }
        public PlayerState OpponentPlayer { get; }

        public string ActingPlayerKey { get; }
        public string OpponentPlayerKey { get; }

        public IReadOnlyList<CardRuntimeState> HandCards { get; }
        public IBoardStateReader BoardReader { get; }

        public ComputerGameSnapshot(
            GameManager gameManager,
            HexGrid hexGrid,
            GamePhase currentPhase,
            int currentTurn,
            PlayerState actingPlayer,
            PlayerState opponentPlayer,
            string actingPlayerKey,
            string opponentPlayerKey,
            IReadOnlyList<CardRuntimeState> handCards,
            IBoardStateReader boardReader)
        {
            GameManager = gameManager;
            HexGrid = hexGrid;
            CurrentPhase = currentPhase;
            CurrentTurn = currentTurn;
            ActingPlayer = actingPlayer;
            OpponentPlayer = opponentPlayer;
            ActingPlayerKey = actingPlayerKey;
            OpponentPlayerKey = opponentPlayerKey;
            HandCards = handCards;
            BoardReader = boardReader;
        }
    }
}
