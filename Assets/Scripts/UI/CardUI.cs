using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

namespace FortGame.UI
{
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Card Visuals")]
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI costText;
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public CardRuntimeState runtimeCard;

        [Header("Selection Visuals")]
        public Color selectedColor = new Color(1f, 1f, 0f, 1f);

        // ── Private state ─────────────────────────────────────────────────
        private GameManager _gameManager;
        private HUDManager _hudManager;
        private Image _imageComponent;
        private Color _originalColor;
        private bool _isSelected;

        public Action<CardUI> clickOverride;

        public string CardName => cardNameText?.text ?? "Unknown";
        public bool IsSelected => _isSelected;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            _imageComponent = GetComponent<Image>();
            if (_imageComponent != null)
                _originalColor = _imageComponent.color;

            _gameManager = FindFirstObjectByType<GameManager>();
            _hudManager  = FindFirstObjectByType<HUDManager>();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (_imageComponent != null)
            {
                _imageComponent.color = selected ? selectedColor : _originalColor;
            }

            if (runtimeCard != null && !runtimeCard.IsManifestedOnBoard)
            {
                if (selected)
                {
                    BoardCardPreviewUI.ShowForCard(runtimeCard);
                }
                else
                {
                    BoardCardPreviewUI.HideActive();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isSelected)
                transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickOverride != null)
            {
                clickOverride(this);
                return;
            }

            RevivalManager revivalManager = RevivalManager.Instance ?? RevivalManager.GetOrCreate();
            if (revivalManager != null && revivalManager.BlocksCardSelection(this))
            {
                _hudManager?.ShowInfo("Finish or cancel Revival before selecting another card.");
                return;
            }

            if (_gameManager != null
                && _gameManager.currentPhase == GamePhase.Buy
                && !_gameManager.isBuyDecisionPending)
            {
                SelectForDiscard();
                return;
            }

            bool selected = CardSelectionManager.Instance?.TrySelectCard(this) ?? false;
            if (selected)
            {
                if (runtimeCard != null && revivalManager != null && revivalManager.TryBeginFromHand(runtimeCard))
                    return;

                TargetSelectionManager.Instance?.ShowValidTargets(this);
                return;
            }

            if (!(CardSelectionManager.Instance?.HasSelection ?? false))
                TargetSelectionManager.Instance?.OnSelectionCancelled();
        }

        private void SelectForDiscard()
        {
            if (runtimeCard == null)
            {
                _hudManager?.ShowError("This card is missing game data.");
                Debug.Log("This UI card has no runtime card linked.");
                return;
            }

            _gameManager.SelectCardToDiscard(runtimeCard);
            if (_gameManager.handUI != null)
                _gameManager.handUI.ClearVisualSelection();

            SetSelected(true);
            _hudManager?.SetSelectedCard(CardName);
            _hudManager?.ShowInfo($"{CardName} selected for discard.");
            Debug.Log("Card selected for discard.");
        }
    }
}
