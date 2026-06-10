using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FortGame.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class StatsPanelUI : MonoBehaviour
    {
        [Header("UI References (Assign in Inspector)")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI statsText;
        public CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [Tooltip("If true, the panel flies from the clicked card's position. If false, it uses startingOffset.")]
        public bool flyFromCard = true;

        [Tooltip("Starting position offset relative to target position when flyFromCard is false.")]
        public Vector2 startingOffset = new Vector2(0f, -50f);

        private Vector2 _targetAnchoredPos;
        private RectTransform _rectTransform;
        private Coroutine _animCoroutine;
        private bool _isInitialized;
        private bool _isDynamicFallback;
        private Canvas _canvas;

        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static StatsPanelUI GetOrCreate(Canvas canvas)
        {
            if (canvas == null) return null;

            // 1. First, search for a designer-configured StatsPanel in the Canvas hierarchy (even if inactive/nested)
            Transform panelTransform = FindDeepChild(canvas.transform, "StatsPanel");
            if (panelTransform != null)
            {
                StatsPanelUI panel = panelTransform.GetComponent<StatsPanelUI>();
                if (panel == null)
                {
                    panel = panelTransform.gameObject.AddComponent<StatsPanelUI>();
                }
                panel.Initialize();
                return panel;
            }

            // 2. Fallback: Search the active scene objects
            GameObject panelObj = GameObject.Find("StatsPanel");
            if (panelObj != null)
            {
                StatsPanelUI panel = panelObj.GetComponent<StatsPanelUI>();
                if (panel == null)
                {
                    panel = panelObj.AddComponent<StatsPanelUI>();
                }
                panel.Initialize();
                return panel;
            }

            // 3. Fallback: Create a dynamic StatsPanel programmatically
            GameObject newPanelObj = new GameObject("StatsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(StatsPanelUI));
            newPanelObj.transform.SetParent(canvas.transform, false);

            StatsPanelUI dynamicPanel = newPanelObj.GetComponent<StatsPanelUI>();
            dynamicPanel._isDynamicFallback = true;
            dynamicPanel.Initialize();
            return dynamicPanel;
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _targetAnchoredPos = _rectTransform.anchoredPosition;
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 260;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // If it's a programmatically created fallback, initialize its UI structure
            if (_isDynamicFallback)
            {
                InitializeDynamicUI();
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
            _isInitialized = true;
        }

        private void InitializeDynamicUI()
        {
            _rectTransform.anchorMin = new Vector2(1f, 0.5f); // Right center fallback
            _rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = new Vector2(250f, 180f);

            Image image = GetComponent<Image>();
            if (image != null)
            {
                Sprite sprite = Resources.Load<Sprite>("UI/HUD/Stats_Panel");
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Sliced;
                }
                else
                {
                    image.color = new Color(0.12f, 0.15f, 0.20f, 0.95f);
                }
            }

            // Create stats text (expanded to fill the panel since title is removed)
            GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(transform, false);
            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.pivot = new Vector2(0.5f, 0.5f);
            statsRect.offsetMin = new Vector2(20f, 15f);
            statsRect.offsetMax = new Vector2(-20f, -15f);

            statsText = statsObj.GetComponent<TextMeshProUGUI>();
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.fontSize = 16f;
            statsText.color = Color.white;
            statsText.lineSpacing = 6f;
            statsText.text = "Stats info";
            ApplyFont(statsText);
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            if (text == null) return;
            TextMeshProUGUI templateText = FindFirstObjectByType<TextMeshProUGUI>();
            if (templateText != null)
            {
                text.font = templateText.font;
                text.fontSharedMaterial = templateText.fontSharedMaterial;
            }
        }

        public void Show(CardRuntimeState card, Vector3 cardStartWorldPos, float duration)
        {
            Initialize();

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            gameObject.SetActive(true);
            PopulateStats(card);

            _animCoroutine = StartCoroutine(AnimateShow(cardStartWorldPos, duration));
        }

        public void ShowFromRight(CardRuntimeState card, float duration)
        {
            Initialize();

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            gameObject.SetActive(true);
            PopulateStats(card);

            _animCoroutine = StartCoroutine(AnimateShowFromRight(duration));
        }

        public void Hide(float duration)
        {
            Initialize();

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(AnimateHide(duration));
        }

        private void PopulateStats(CardRuntimeState card)
        {
            if (statsText == null) return;

            if (card == null || card.SourceCard == null)
            {
                statsText.text = string.Empty;
                return;
            }

            CardData data = card.SourceCard;
            if (data is CharacterCardData charData)
            {
                string speedText = charData.unitMovementCapacity.HasValue ? charData.unitMovementCapacity.Value.ToString() : "-";
                statsText.text = $"HP : {charData.maxHp}\nDamage : {charData.attackDamage}\nRange : {charData.attackRange}\nMovement : {speedText}";
            }
            else if (data is WorldEffectCardData weData)
            {
                string hpText = weData.structureHp.HasValue ? weData.structureHp.Value.ToString() : "-";
                string dmgText = weData.structureDamage.HasValue ? weData.structureDamage.Value.ToString() : "-";
                string rangeText = weData.worldEffectAttackRange.HasValue ? weData.worldEffectAttackRange.Value.ToString() : "-";
                statsText.text = $"HP : {hpText}\nDamage : {dmgText}\nRange : {rangeText}";
            }
            else if (data is SpellCardData spellData)
            {
                statsText.text = $"Damage : {spellData.effectPower}\nDuration : {spellData.effectDurationTurns}";
            }
            else
            {
                statsText.text = data.description;
            }
        }

        private IEnumerator AnimateShow(Vector3 cardStartWorldPos, float duration)
        {
            RectTransform canvasRect = _rectTransform.parent as RectTransform;

            Vector2 startAnchoredPos;
            if (flyFromCard && canvasRect != null)
            {
                Vector3 localStartPos = canvasRect.InverseTransformPoint(cardStartWorldPos);
                startAnchoredPos = new Vector2(localStartPos.x, localStartPos.y);
            }
            else
            {
                startAnchoredPos = _targetAnchoredPos + startingOffset;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, _targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }

            _rectTransform.anchoredPosition = _targetAnchoredPos;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private IEnumerator AnimateHide(float duration)
        {
            Vector2 startAnchoredPos = _rectTransform.anchoredPosition;
            Vector2 targetAnchoredPos = _targetAnchoredPos + startingOffset;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, ease);
                yield return null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator AnimateShowFromRight(float duration)
        {
            Vector2 startAnchoredPos = _targetAnchoredPos + new Vector2(700f, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                _rectTransform.anchoredPosition = Vector2.Lerp(startAnchoredPos, _targetAnchoredPos, ease);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, ease);
                yield return null;
            }

            _rectTransform.anchoredPosition = _targetAnchoredPos;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }
    }
}
