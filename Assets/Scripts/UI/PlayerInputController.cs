using FortGame.Computer;
using UnityEngine;

namespace FortGame.UI
{
    /// <summary>
    /// Bridges player UI interactions with game state and action execution.
    /// </summary>
    public sealed class PlayerInputController : MonoBehaviour
    {
        public static PlayerInputController Instance { get; private set; }

        private CardSelectionManager _cardSelectionMgr;
        private TargetSelectionManager _targetSelectionMgr;
        private HUDManager _hudManager;
        private GameManager _gameManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _cardSelectionMgr = FindFirstObjectByType<CardSelectionManager>();
            _targetSelectionMgr = FindFirstObjectByType<TargetSelectionManager>();
            _hudManager = FindFirstObjectByType<HUDManager>();
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        private void Update()
        {
            // Phase change clears selection
            if (_cardSelectionMgr != null && _gameManager != null)
            {
                if (_gameManager.currentPhase != GamePhase.Play && _cardSelectionMgr.HasSelection)
                {
                    _cardSelectionMgr.ClearSelection();
                    _targetSelectionMgr?.OnSelectionCancelled();
                }
            }

            // ESC key to cancel selection
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_cardSelectionMgr?.HasSelection ?? false)
                {
                    _cardSelectionMgr.CancelSelection();
                    _targetSelectionMgr?.OnSelectionCancelled();
                    Debug.Log("[PlayerInputController] Selection cancelled by ESC.");
                }
            }
        }

        /// <summary>
        /// Called when a card's target is confirmed by the player.
        /// </summary>
        public void OnTargetConfirmed(CardTarget target)
        {
            CardUI selectedCard = _cardSelectionMgr?.SelectedCard;
            if (selectedCard == null)
            {
                return;
            }

            // Validate target using legal action service
            bool targetIsLegal = false;
            var legalActions = LegalActionService.Instance.GetLegalActions(_gameManager?.currentPlayer as FortGame.Computer.ComputerPlayer);
            
            foreach (var action in legalActions)
            {
                if (action.sourceCardName == selectedCard.CardName && 
                    action.target.type == target.type && 
                    action.target.tile.q == target.tile.q &&
                    action.target.tile.r == target.tile.r)
                {
                    targetIsLegal = true;
                    ExecuteAction(action);
                    break;
                }
            }

            if (!targetIsLegal)
            {
                _hudManager?.ShowError("Invalid target for this card.");
                Debug.Log("[PlayerInputController] Target validation failed.");
            }
        }

        private void ExecuteAction(FortGame.Computer.ComputerAction action)
        {
            if (action == null)
            {
                return;
            }

            var executor = new ComputerActionExecutor();
            var snapshotProvider = new ComputerGameSnapshotProvider();
            
            // Create snapshot and execute
            // Note: This is temporary until full card effect pipeline is ready from Fatine
            
            Debug.Log($"[PlayerInputController] Executing action: {action.actionName}");

            _cardSelectionMgr.ClearSelection();
            _targetSelectionMgr?.OnSelectionCancelled();

            _hudManager?.ShowError(""); // Clear any error
        }
    }
}
