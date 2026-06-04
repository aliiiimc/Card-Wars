using FortGame.Computer;
using FortGame.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class HotseatCardTestController : MonoBehaviour
{
    private const string TestSceneName = "CardBehaviorTest";

    private GameManager _gameManager;
    private HandUI _handUI;
    private CardLibrary _cardLibrary;
    private HUDManager _hudManager;
    private Canvas _uiCanvas;
    private RectTransform _cardListRoot;
    private TextMeshProUGUI _cardListTitle;
    private RectTransform _characterContent;
    private RectTransform _spellContent;
    private RectTransform _worldEffectContent;
    private TextMeshProUGUI _characterTitle;
    private TextMeshProUGUI _spellTitle;
    private TextMeshProUGUI _worldEffectTitle;
    private ScrollRect _characterScrollRect;
    private ScrollRect _spellScrollRect;
    private ScrollRect _worldEffectScrollRect;
    private TMP_FontAsset _debugFont;
    private PlayerState _preparedPlayer;
    private PlayerState _renderedPlayer;
    private int _renderedHandCount = -1;
    private bool _initializedSceneState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForTestScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != TestSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<HotseatCardTestController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject(nameof(HotseatCardTestController));
        controllerObject.AddComponent<HotseatCardTestController>();
    }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != TestSceneName)
        {
            Destroy(gameObject);
            return;
        }

        DisableComputerOpponent();
    }

    private void Update()
    {
        if (!TryInitializeReferences())
        {
            return;
        }

        DisableComputerOpponent();

        if (!_initializedSceneState)
        {
            PreparePlayer(_gameManager.player1);
            PreparePlayer(_gameManager.player2);
            RenderCurrentPlayerHand();
            _initializedSceneState = true;
        }

        if (_gameManager.currentPhase == GamePhase.GameOver || _gameManager.currentPlayer == null)
        {
            return;
        }

        if (_gameManager.currentPhase == GamePhase.Buy || !ReferenceEquals(_preparedPlayer, _gameManager.currentPlayer))
        {
            PrepareTurnForCurrentPlayer();
        }

        RenderCurrentPlayerCardButtons(forceRefresh: false);
    }

    private bool TryInitializeReferences()
    {
        if (_gameManager == null)
        {
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        if (_handUI == null && _gameManager != null)
        {
            _handUI = _gameManager.handUI;
        }

        if (_handUI == null)
        {
            _handUI = FindFirstObjectByType<HandUI>();
        }

        if (_cardLibrary == null && _gameManager != null)
        {
            _cardLibrary = _gameManager.cardLibrary;
        }

        if (_hudManager == null)
        {
            _hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (_uiCanvas == null)
        {
            _uiCanvas = FindFirstObjectByType<Canvas>();
        }

        if (_debugFont == null)
        {
            _debugFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        return _gameManager != null
            && _gameManager.player1 != null
            && _gameManager.player2 != null
            && _gameManager.currentPlayer != null
            && _cardLibrary != null;
    }

    private void DisableComputerOpponent()
    {
        ComputerPlayer[] computerPlayers = FindObjectsByType<ComputerPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < computerPlayers.Length; i++)
        {
            ComputerPlayer computerPlayer = computerPlayers[i];
            if (computerPlayer != null && computerPlayer.gameObject.activeSelf)
            {
                computerPlayer.gameObject.SetActive(false);
            }
        }

        if (_gameManager == null)
        {
            _gameManager = FindFirstObjectByType<GameManager>();
        }

        if (_gameManager != null)
        {
            _gameManager.computerPlayer = null;
        }
    }

    private void PrepareTurnForCurrentPlayer()
    {
        PreparePlayer(_gameManager.currentPlayer);
        _gameManager.hasBoughtThisTurn = false;
        _gameManager.discardCardsUsedThisTurn = 0;
        _gameManager.isBuyDecisionPending = false;
        _gameManager.mustDiscardAfterBuy = false;
        _gameManager.pendingBuyCost = -1;
        _gameManager.ClearSelectedCardToDiscard();
        _gameManager.currentPhase = GamePhase.Play;

        UnitManager unitManager = FindFirstObjectByType<UnitManager>();
        unitManager?.ResetUnitsForCurrentOwnerTurn(force: true);

        CardSelectionManager.Instance?.ClearSelection();
        TargetSelectionManager.Instance?.OnSelectionCancelled();

        RenderCurrentPlayerCardButtons(forceRefresh: true);
        _gameManager.RefreshHUD();
        UpdateTurnLabel();
        _preparedPlayer = _gameManager.currentPlayer;
    }

    private void PreparePlayer(PlayerState player)
    {
        if (player == null)
        {
            return;
        }

        player.handCards.Clear();

        List<CardData> allCards = GetAllCardDefinitions();
        for (int i = 0; i < allCards.Count; i++)
        {
            CardData cardData = allCards[i];
            if (cardData == null)
            {
                continue;
            }

            CardRuntimeState runtimeCard = CardFactory.CreateRuntimeState(cardData);
            if (runtimeCard != null)
            {
                player.handCards.Add(runtimeCard);
            }
        }

        player.handCount = player.handCards.Count;
        player.maxHandSize = Mathf.Max(player.maxHandSize, player.handCount);
    }

    private void RenderCurrentPlayerHand()
    {
        if (_gameManager == null || _gameManager.currentPlayer == null)
        {
            return;
        }

        if (_handUI != null && _handUI.gameObject.activeSelf)
        {
            _handUI.gameObject.SetActive(false);
        }

        RenderCurrentPlayerCardButtons(forceRefresh: true);
    }

    private void RenderCurrentPlayerCardButtons(bool forceRefresh)
    {
        if (_gameManager == null || _gameManager.currentPlayer == null)
        {
            return;
        }

        if (!forceRefresh
            && ReferenceEquals(_renderedPlayer, _gameManager.currentPlayer)
            && _renderedHandCount == _gameManager.currentPlayer.handCards.Count)
        {
            return;
        }

        if (!EnsureDebugCardPanel())
        {
            return;
        }

        ClearChildren(_characterContent);
        ClearChildren(_spellContent);
        ClearChildren(_worldEffectContent);

        int characterCount = 0;
        int spellCount = 0;
        int worldEffectCount = 0;

        for (int i = 0; i < _gameManager.currentPlayer.handCards.Count; i++)
        {
            CardRuntimeState runtimeCard = _gameManager.currentPlayer.handCards[i];
            if (runtimeCard?.SourceCard is CharacterCardData)
            {
                CreateCardButton(runtimeCard, _characterContent);
                characterCount++;
                continue;
            }

            if (runtimeCard?.SourceCard is SpellCardData)
            {
                CreateCardButton(runtimeCard, _spellContent);
                spellCount++;
                continue;
            }

            if (runtimeCard?.SourceCard is WorldEffectCardData)
            {
                CreateCardButton(runtimeCard, _worldEffectContent);
                worldEffectCount++;
            }
        }

        _renderedPlayer = _gameManager.currentPlayer;
        _renderedHandCount = _gameManager.currentPlayer.handCards.Count;
        if (_cardListTitle != null)
        {
            _cardListTitle.text = $"{_gameManager.currentPlayer.playerName} cards ({_renderedHandCount})";
        }

        SetCategoryTitle(_characterTitle, "Characters", characterCount);
        SetCategoryTitle(_spellTitle, "Spells", spellCount);
        SetCategoryTitle(_worldEffectTitle, "World Effects", worldEffectCount);
        ResetScrollPosition(_characterScrollRect);
        ResetScrollPosition(_spellScrollRect);
        ResetScrollPosition(_worldEffectScrollRect);
    }

    private void UpdateTurnLabel()
    {
        if (_hudManager != null && _hudManager.currentPlayerText != null && _gameManager.currentPlayer != null)
        {
            _hudManager.currentPlayerText.text = $"{_gameManager.currentPlayer.playerName} turn";
        }
    }

    private bool EnsureDebugCardPanel()
    {
        if (_cardListRoot != null
            && _characterContent != null
            && _spellContent != null
            && _worldEffectContent != null)
        {
            return true;
        }

        if (_uiCanvas == null)
        {
            _uiCanvas = FindFirstObjectByType<Canvas>();
        }

        if (_uiCanvas == null)
        {
            return false;
        }

        GameObject panelObject = new GameObject("HotseatCardListPanel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(_uiCanvas.transform, false);
        _cardListRoot = panelObject.GetComponent<RectTransform>();
        _cardListRoot.anchorMin = new Vector2(0f, 0f);
        _cardListRoot.anchorMax = new Vector2(1f, 0f);
        _cardListRoot.pivot = new Vector2(0.5f, 0f);
        _cardListRoot.sizeDelta = new Vector2(-24f, 360f);
        _cardListRoot.anchoredPosition = new Vector2(0f, 12f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

        GameObject titleObject = CreateUiObject("Title", _cardListRoot);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-24f, 48f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);

        _cardListTitle = titleObject.AddComponent<TextMeshProUGUI>();
        _cardListTitle.font = _debugFont;
        _cardListTitle.fontSize = 28f;
        _cardListTitle.color = Color.white;
        _cardListTitle.alignment = TextAlignmentOptions.Center;
        _cardListTitle.text = "Cards";

        GameObject columnsObject = CreateUiObject("Columns", _cardListRoot);
        RectTransform columnsRect = columnsObject.GetComponent<RectTransform>();
        columnsRect.anchorMin = new Vector2(0f, 0f);
        columnsRect.anchorMax = new Vector2(1f, 1f);
        columnsRect.offsetMin = new Vector2(12f, 12f);
        columnsRect.offsetMax = new Vector2(-12f, -70f);

        HorizontalLayoutGroup columnsLayout = columnsObject.AddComponent<HorizontalLayoutGroup>();
        columnsLayout.spacing = 12f;
        columnsLayout.padding = new RectOffset(0, 0, 0, 0);
        columnsLayout.childAlignment = TextAnchor.UpperCenter;
        columnsLayout.childControlHeight = true;
        columnsLayout.childControlWidth = true;
        columnsLayout.childForceExpandHeight = true;
        columnsLayout.childForceExpandWidth = true;

        CreateCategoryColumn(columnsRect, "Characters", out _characterTitle, out _characterScrollRect, out _characterContent);
        CreateCategoryColumn(columnsRect, "Spells", out _spellTitle, out _spellScrollRect, out _spellContent);
        CreateCategoryColumn(columnsRect, "World Effects", out _worldEffectTitle, out _worldEffectScrollRect, out _worldEffectContent);
        return true;
    }

    private void CreateCardButton(CardRuntimeState runtimeCard, RectTransform parent)
    {
        if (runtimeCard == null || runtimeCard.SourceCard == null || parent == null)
        {
            return;
        }

        GameObject buttonObject = CreateUiObject(runtimeCard.SourceCard.DisplayName, parent);
        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 56f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.20f, 0.24f, 0.31f, 1f);

        CardUI cardUi = buttonObject.AddComponent<CardUI>();
        cardUi.runtimeCard = runtimeCard;
        cardUi.selectedColor = new Color(0.96f, 0.78f, 0.26f, 1f);

        GameObject labelObject = CreateUiObject("Label", buttonObject.GetComponent<RectTransform>());
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 8f);
        labelRect.offsetMax = new Vector2(-14f, -8f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = _debugFont;
        label.fontSize = 28f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = runtimeCard.SourceCard.DisplayName;

        cardUi.cardNameText = label;
    }

    private void CreateCategoryColumn(
        RectTransform parent,
        string titleText,
        out TextMeshProUGUI titleLabel,
        out ScrollRect scrollRect,
        out RectTransform contentRoot)
    {
        GameObject columnObject = CreateUiObject(titleText + " Column", parent);
        LayoutElement columnLayout = columnObject.AddComponent<LayoutElement>();
        columnLayout.flexibleWidth = 1f;

        Image columnImage = columnObject.AddComponent<Image>();
        columnImage.color = new Color(0.14f, 0.16f, 0.20f, 0.94f);

        VerticalLayoutGroup columnGroup = columnObject.AddComponent<VerticalLayoutGroup>();
        columnGroup.spacing = 8f;
        columnGroup.padding = new RectOffset(8, 8, 8, 8);
        columnGroup.childAlignment = TextAnchor.UpperCenter;
        columnGroup.childControlHeight = true;
        columnGroup.childControlWidth = true;
        columnGroup.childForceExpandHeight = false;
        columnGroup.childForceExpandWidth = true;

        GameObject titleObject = CreateUiObject("Title", columnObject.transform);
        LayoutElement titleLayout = titleObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 34f;

        titleLabel = titleObject.AddComponent<TextMeshProUGUI>();
        titleLabel.font = _debugFont;
        titleLabel.fontSize = 22f;
        titleLabel.color = Color.white;
        titleLabel.alignment = TextAlignmentOptions.Center;
        titleLabel.text = titleText;

        GameObject scrollObject = CreateUiObject("Scroll View", columnObject.transform);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 230f;

        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(0.10f, 0.11f, 0.16f, 0.95f);
        scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject viewportObject = CreateUiObject("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = CreateUiObject("Content", viewportRect);
        contentRoot = contentObject.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
    }

    private List<CardData> GetAllCardDefinitions()
    {
        List<CardData> cards = new List<CardData>();
        HashSet<CardData> seenCards = new HashSet<CardData>();

        if (_cardLibrary != null && _cardLibrary.cards != null)
        {
            for (int i = 0; i < _cardLibrary.cards.Count; i++)
            {
                TryAddCard(cards, seenCards, _cardLibrary.cards[i]);
            }
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/ScriptableObjects/Cards" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            CardData cardAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
            TryAddCard(cards, seenCards, cardAsset);
        }
#endif

        cards.Sort((left, right) =>
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int costComparison = left.cost.CompareTo(right.cost);
            if (costComparison != 0)
            {
                return costComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.OrdinalIgnoreCase);
        });

        return cards;
    }

    private static void TryAddCard(List<CardData> cards, HashSet<CardData> seenCards, CardData cardData)
    {
        if (cardData == null || seenCards.Contains(cardData))
        {
            return;
        }

        seenCards.Add(cardData);
        cards.Add(cardData);
    }

    private static void SetCategoryTitle(TextMeshProUGUI label, string categoryName, int count)
    {
        if (label != null)
        {
            label.text = $"{categoryName} ({count})";
        }
    }

    private static void ResetScrollPosition(ScrollRect scrollRect)
    {
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
