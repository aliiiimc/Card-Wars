using UnityEngine;

namespace FortGame.UI
{
    /// <summary>
    /// Manages the card selection state machine.
    /// Handles: Idle -> CardSelected -> WaitingForTarget -> Confirmed/Cancelled
    /// </summary>
    public sealed class CardSelectionManager : MonoBehaviour
    {
        public static CardSelectionManager Instance { get; private set; }

        private CardSelectionState _currentState = CardSelectionState.Idle;
        private CardUI _selectedCard;
        private GameManager _gameManager;

        public CardSelectionState CurrentState => _currentState;
        public CardUI SelectedCard => _selectedCard;
        public bool HasSelection => _selectedCard != null && _currentState != CardSelectionState.Idle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        /// <summary>
        /// Attempts to select a card. If already selected, deselects instead.
        /// </summary>
        public bool TrySelectCard(CardUI card)
        {
            if (card == null)
            {
                return false;
            }

            // Check phase gate
            if (_gameManager != null && _gameManager.currentPhase != GamePhase.Play)
            {
                Debug.Log("[CardSelectionManager] Cannot select cards outside Play phase.");
                return false;
            }

            // If same card is clicked again, deselect
            if (_selectedCard == card)
            {
                CancelSelection();
                return false;
            }

            // If different card selected, deselect old and select new
            if (_selectedCard != null)
            {
                _selectedCard.SetSelected(false);
            }

            _selectedCard = card;
            _currentState = CardSelectionState.CardSelected;
            card.SetSelected(true);

            Debug.Log($"[CardSelectionManager] Card selected: {card.CardName}");

            return true;
        }

        /// <summary>
        /// Cancels the current selection.
        /// </summary>
        public void CancelSelection()
        {
            if (_selectedCard != null)
            {
                _selectedCard.SetSelected(false);
                Debug.Log($"[CardSelectionManager] Card deselected: {_selectedCard.CardName}");
            }

            _selectedCard = null;
            _currentState = CardSelectionState.Idle;
        }

        /// <summary>
        /// Transitions to waiting-for-target state.
        /// </summary>
        public void EnterTargetSelection()
        {
            if (_selectedCard == null)
            {
                Debug.LogWarning("[CardSelectionManager] No card selected for target selection.");
                return;
            }

            _currentState = CardSelectionState.WaitingForTarget;
            Debug.Log("[CardSelectionManager] Waiting for target...");
        }

        /// <summary>
        /// Confirms the selection with a target.
        /// </summary>
        public void ConfirmSelection(CardTarget target)
        {
            if (_selectedCard == null)
            {
                return;
            }

            _currentState = CardSelectionState.Confirmed;
            Debug.Log($"[CardSelectionManager] Selection confirmed with target: {target.type}");
        }

        /// <summary>
        /// Clears selection (called when phase changes or turn ends).
        /// </summary>
        public void ClearSelection()
        {
            CancelSelection();
            _currentState = CardSelectionState.Idle;
        }
    }
}
