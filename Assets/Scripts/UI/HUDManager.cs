using UnityEngine;
using TMPro; // Standard Unity text package

namespace FortGame.UI 
{
    /// <summary>
    /// Manages the heads-up display showing the player's Fort HP, Resources/Money, and game messages.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI fortHpText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI turnStatusText;
        public TextMeshProUGUI selectedCardText;
        public TextMeshProUGUI infoMessageText;
        public TextMeshProUGUI errorMessageText;
        public float errorMessageDuration = 3f;

        private float _errorMessageTimer = 0f;

        private void Update()
        {
            // Fade out error message after duration
            if (_errorMessageTimer > 0)
            {
                _errorMessageTimer -= Time.deltaTime;
                if (_errorMessageTimer <= 0 && errorMessageText != null)
                {
                    errorMessageText.text = "";
                }
            }
        }

        /// <summary>
        /// Updates the HUD to reflect the current state of the provided PlayerState.
        /// </summary>
        public void UpdateHUD(PlayerState state)
        {
            if (state == null) return;

            if (playerNameText != null)
                playerNameText.text = state.playerName;

            if (fortHpText != null)
                fortHpText.text = $"Fort HP: {state.fortHp}";

            if (moneyText != null)
                moneyText.text = $"Money: {state.money}";
        }

        /// <summary>
        /// Call this when the turn changes (e.g., "Your Turn!" or "Enemy Turn")
        /// </summary>
        public void SetTurnStatus(string statusMessage)
        {
            if (turnStatusText != null)
            {
                turnStatusText.text = statusMessage;
            }
        }

        public void SetSelectedCard(string cardName)
        {
            if (selectedCardText != null)
            {
                selectedCardText.text = string.IsNullOrWhiteSpace(cardName)
                    ? ""
                    : $"Selected: {cardName}";
            }
        }

        public void ShowInfo(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
            else if (errorMessageText != null)
            {
                errorMessageText.text = message;
                _errorMessageTimer = 0f;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                Debug.Log($"[HUDManager] Info: {message}");
            }
        }

        public void ClearFeedback()
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = "";
            }

            if (errorMessageText != null)
            {
                errorMessageText.text = "";
            }

            _errorMessageTimer = 0f;
        }

        /// <summary>
        /// Display an error message to the player.
        /// </summary>
        public void ShowError(string message)
        {
            if (errorMessageText != null)
            {
                errorMessageText.text = message;
                _errorMessageTimer = string.IsNullOrWhiteSpace(message) ? 0f : errorMessageDuration;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    Debug.Log($"[HUDManager] Error: {message}");
                }
            }
        }
    }
}
