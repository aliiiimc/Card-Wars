using UnityEngine;
using UnityEngine.UI;
using TMPro; // Standard Unity text package

namespace FortGame.UI 
{
    /// <summary>
    /// Manages the heads-up display showing the player's Fort HP, Resources/Money, and game messages.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Legacy UI Elements")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI fortHpText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI turnStatusText;
        public TextMeshProUGUI selectedCardText;
        public TextMeshProUGUI infoMessageText;
        public TextMeshProUGUI errorMessageText;
        public float errorMessageDuration = 3f;

        [Header("Player Panel")]
        public TextMeshProUGUI playerFortHpText;
        public TextMeshProUGUI playerMoneyText;
        public TextMeshProUGUI playerCardsText;
        public Image playerHpFill;

        [Header("Enemy Panel")]
        public TextMeshProUGUI enemyFortHpText;
        public TextMeshProUGUI enemyMoneyText;
        public TextMeshProUGUI enemyCardsText;
        public Image enemyHpFill;

        [Header("Turn Panel")]
        public TextMeshProUGUI currentRoundText;
        public TextMeshProUGUI currentPlayerText;

        private float _errorMessageTimer = 0f;

        private void Awake()
        {
            AutoBindMissingReferences();
        }

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

        public void UpdateHUD(PlayerState player, PlayerState enemy, PlayerState currentPlayer, GamePhase phase, int maxFortHp, int roundNumber)
        {
            AutoBindMissingReferences();

            UpdatePanel(player, playerFortHpText, playerMoneyText, playerCardsText, playerHpFill, maxFortHp);
            UpdatePanel(enemy, enemyFortHpText, enemyMoneyText, enemyCardsText, enemyHpFill, maxFortHp);

            if (currentRoundText != null)
            {
                currentRoundText.text = $"ROUND {Mathf.Max(1, roundNumber)}";
            }

            if (currentPlayerText != null)
            {
                if (phase == GamePhase.GameOver)
                {
                    currentPlayerText.text = "Game Over";
                }
                else if (currentPlayer != null && player != null && ReferenceEquals(currentPlayer, player))
                {
                    currentPlayerText.text = "Your turn";
                }
                else if (currentPlayer != null && enemy != null && ReferenceEquals(currentPlayer, enemy))
                {
                    currentPlayerText.text = "Enemy turn";
                }
                else
                {
                    currentPlayerText.text = "";
                }
            }

            if (turnStatusText != null && currentPlayer != null)
            {
                turnStatusText.text = $"{currentPlayer.playerName} - {phase}";
            }
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

        private void UpdatePanel(
            PlayerState state,
            TextMeshProUGUI fortHpValue,
            TextMeshProUGUI moneyValue,
            TextMeshProUGUI cardsValue,
            Image hpFill,
            int maxFortHp)
        {
            if (state == null)
            {
                return;
            }

            int safeMaxFortHp = Mathf.Max(1, maxFortHp);

            if (fortHpValue != null)
            {
                fortHpValue.text = $"{state.fortHp}/{safeMaxFortHp}";
            }

            if (moneyValue != null)
            {
                moneyValue.text = state.money.ToString();
            }

            if (cardsValue != null)
            {
                cardsValue.text = $"{state.handCount}/{state.maxHandSize}";
            }

            if (hpFill != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(state.fortHp / (float)safeMaxFortHp);
            }
        }

        private void AutoBindMissingReferences()
        {
            if (playerFortHpText == null) playerFortHpText = FindComponentByObjectName<TextMeshProUGUI>("PlayerFortHpText");
            if (playerMoneyText == null) playerMoneyText = FindComponentByObjectName<TextMeshProUGUI>("PlayerMoneyText");
            if (playerCardsText == null) playerCardsText = FindComponentByObjectName<TextMeshProUGUI>("PlayerCardsText");
            if (playerHpFill == null) playerHpFill = FindComponentByObjectName<Image>("PlayerHpFill");

            if (enemyFortHpText == null) enemyFortHpText = FindComponentByObjectName<TextMeshProUGUI>("EnemyFortHpText");
            if (enemyMoneyText == null) enemyMoneyText = FindComponentByObjectName<TextMeshProUGUI>("EnemyMoneyText");
            if (enemyCardsText == null) enemyCardsText = FindComponentByObjectName<TextMeshProUGUI>("EnemyCardsText");
            if (enemyHpFill == null) enemyHpFill = FindComponentByObjectName<Image>("EnemyHpFill");

            if (currentRoundText == null) currentRoundText = FindComponentByObjectName<TextMeshProUGUI>("CurrentRoundText");
            if (currentPlayerText == null) currentPlayerText = FindComponentByObjectName<TextMeshProUGUI>("CurrentPlayerText");
        }

        private T FindComponentByObjectName<T>(string objectName) where T : Component
        {
            Transform child = FindChildByName(transform, objectName);
            if (child != null && child.TryGetComponent(out T localComponent))
            {
                return localComponent;
            }

            T[] components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].name == objectName)
                {
                    return components[i];
                }
            }

            return null;
        }

        private Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }
    }
}
