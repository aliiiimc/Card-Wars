using FortGame.Computer;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FortGame.UI
{
    public sealed class BoardCardPreviewUI : MonoBehaviour
    {
        private const string PreviewObjectName = "BoardCardPreview";
        private static BoardCardPreviewUI _activePreview;

        private Canvas canvas;
        private RectTransform rectTransform;
        private Image cardImage;
        private StatsPanelUI statsPanel;
        private Canvas previewCanvas;
        private Coroutine animCoroutine;
        private Vector2 targetAnchoredPos;
        private Vector2 configuredAnchoredPos;

        public static BoardCardPreviewUI GetOrCreate(Canvas canvas)
        {
            if (canvas == null) return null;

            BoardCardPreviewUI existing = canvas.GetComponentInChildren<BoardCardPreviewUI>(true);
            if (existing != null)
            {
                existing.Initialize(canvas, false);
                return existing;
            }

            GameObject previewObject = new GameObject(PreviewObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BoardCardPreviewUI));
            previewObject.transform.SetParent(canvas.transform, false);

            // Sync layer with canvas to make sure it renders on the UI layer (Layer 5)
            previewObject.layer = canvas.gameObject.layer;

            BoardCardPreviewUI preview = previewObject.GetComponent<BoardCardPreviewUI>();
            preview.Initialize(canvas, true);
            return preview;
        }

        public static void ShowForTile(HexTile tile)
        {
            if (tile == null) return;

            Canvas targetCanvas = ResolveUiCanvas();
            HexGrid grid = FindFirstObjectByType<HexGrid>();
            if (targetCanvas == null || grid == null)
            {
                return;
            }

            CardRuntimeState card = new HexGridBoardStateReader(grid).GetCardAt(tile.coord);
            BoardCardPreviewUI preview = GetOrCreate(targetCanvas);
            if (preview == null)
            {
                return;
            }

            if (card == null || card.SourceCard == null)
            {
                preview.Hide();
                return;
            }

            preview.Show(card, tile.transform.position);
        }

        public static void ShowForCard(CardRuntimeState card)
        {
            if (card == null || card.SourceCard == null) return;

            Canvas targetCanvas = ResolveUiCanvas();
            if (targetCanvas == null)
            {
                return;
            }

            BoardCardPreviewUI preview = GetOrCreate(targetCanvas);
            if (preview == null)
            {
                return;
            }

            preview.Show(card, Vector3.zero);
        }

        private void Awake()
        {
            Initialize(GetComponentInParent<Canvas>(), false);
        }

        private void Initialize(Canvas ownerCanvas, bool applyDefaultLayout)
        {
            if (canvas == ownerCanvas && rectTransform != null && cardImage != null)
            {
                return;
            }

            canvas = ownerCanvas;
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            if (GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            cardImage = GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = gameObject.AddComponent<Image>();
            }
            cardImage.preserveAspect = true;
            cardImage.raycastTarget = false;
            cardImage.enabled = false;
            HideEmptyChildImages();
            previewCanvas = GetComponent<Canvas>();
            if (previewCanvas == null)
            {
                previewCanvas = gameObject.AddComponent<Canvas>();
            }

            previewCanvas.overrideSorting = true;
            previewCanvas.sortingOrder = 250;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (applyDefaultLayout)
            {
                rectTransform.anchorMin = new Vector2(1f, 0.5f);
                rectTransform.anchorMax = new Vector2(1f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(-149f, -526.4f);
                rectTransform.sizeDelta = new Vector2(180f, 260f);
                rectTransform.localScale = Vector3.one * 3.2f;
            }

            configuredAnchoredPos = rectTransform.anchoredPosition;
            gameObject.SetActive(false);
        }

        private void Show(CardRuntimeState card, Vector3 boardWorldPosition)
        {
            if (card?.SourceCard == null || canvas == null)
            {
                Hide();
                return;
            }

            HideActive();
            _activePreview = this;

            targetAnchoredPos = configuredAnchoredPos;
            HideEmptyChildImages();
            cardImage.sprite = GetPreviewSprite(card.SourceCard);
            cardImage.enabled = cardImage.sprite != null;
            gameObject.SetActive(cardImage.enabled);

            if (gameObject.activeInHierarchy)
            {
                if (animCoroutine != null)
                {
                    StopCoroutine(animCoroutine);
                }

                Vector2 startAnchoredPos = targetAnchoredPos + new Vector2(700f, 0f);
                rectTransform.anchoredPosition = startAnchoredPos;
                animCoroutine = StartCoroutine(AnimateShow(startAnchoredPos, targetAnchoredPos, 0.2f));
            }

            statsPanel = StatsPanelUI.GetOrCreate(canvas);
            if (statsPanel != null)
            {
                statsPanel.gameObject.SetActive(true);
                if (statsPanel.useDynamicPositioning)
                {
                    statsPanel.SyncAnchorsAndPivot(rectTransform);

                    // Calculate position exactly above the preview card, accounting for scale
                    float cardHeight = rectTransform.sizeDelta.y * rectTransform.localScale.y;
                    float panelHeight = statsPanel.GetComponent<RectTransform>().sizeDelta.y * statsPanel.GetComponent<RectTransform>().localScale.y;
                    float offsetY = (cardHeight / 2f) + (panelHeight / 2f) + 15f; // 15px gap

                    statsPanel.SetTargetAnchoredPosition(targetAnchoredPos + new Vector2(0f, offsetY));
                }
            }
            statsPanel?.ShowFromRight(card, 0.2f);
        }

        public void Hide()
        {
            if (animCoroutine != null)
            {
                StopCoroutine(animCoroutine);
                animCoroutine = null;
            }
            if (_activePreview == this)
            {
                _activePreview = null;
            }
            gameObject.SetActive(false);
            statsPanel?.Hide(0.15f);
        }

        public static void HideActive()
        {
            if (_activePreview != null)
            {
                _activePreview.Hide();
            }
        }

        private IEnumerator AnimateShow(Vector2 startAnchoredPos, Vector2 endAnchoredPos, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, endAnchoredPos, ease);
                yield return null;
            }

            rectTransform.anchoredPosition = endAnchoredPos;
            animCoroutine = null;
        }

        private static Canvas ResolveUiCanvas()
        {
            BoardCardPreviewUI existingPreview = Object.FindFirstObjectByType<BoardCardPreviewUI>(FindObjectsInactive.Include);
            Canvas existingCanvas = null;
            if (existingPreview != null && existingPreview.transform.parent != null)
            {
                // On cherche le Canvas à partir du parent pour éviter de trouver celui de BoardCardPreview lui-même
                existingCanvas = existingPreview.transform.parent.GetComponentInParent<Canvas>(true);
            }
            if (existingCanvas != null)
            {
                return existingCanvas;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                if (canvas.GetComponentInChildren<StatsPanelUI>(true) != null)
                {
                    return canvas;
                }
            }

            return Object.FindFirstObjectByType<Canvas>();
        }

        private static Sprite GetPreviewSprite(CardData sourceCard)
        {
            if (sourceCard == null)
            {
                return null;
            }

            if (sourceCard.handDeckSprite != null)
            {
                return sourceCard.handDeckSprite;
            }

            if (sourceCard is CharacterCardData characterCard)
            {
                return characterCard.manifestedSprite;
            }

            if (sourceCard is WorldEffectCardData worldEffectCard)
            {
                return worldEffectCard.manifestedSprite;
            }

            return null;
        }

        private void HideEmptyChildImages()
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null
                    && image != cardImage
                    && image.sprite == null
                    && image.GetComponentInParent<StatsPanelUI>(true) == null
                    && !IsStatsPanelChild(image.transform))
                {
                    image.enabled = false;
                }
            }
        }

        private static bool IsStatsPanelChild(Transform transform)
        {
            while (transform != null)
            {
                if (string.Equals(transform.name, "StatsPanel", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }
    }
}
