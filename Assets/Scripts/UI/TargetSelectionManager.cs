using System.Collections.Generic;
using UnityEngine;
//Ali : needed to read runtime cards from board tiles when a spell targets a unit.
using FortGame.Computer;


namespace FortGame.UI
{
    /// <summary>
    /// Manages target selection after a card is selected.
    /// Shows valid targets and validates player clicks.
    /// </summary>
    public sealed class TargetSelectionManager : MonoBehaviour
    {
        public static TargetSelectionManager Instance { get; private set; }

        private List<HexTile> _highlightedTiles = new List<HexTile>();
        private HexGrid _hexGrid;
        private HUDManager _hudManager;
        private GameManager _gameManager;
        private CardPlayService _cardPlayService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _hexGrid = FindFirstObjectByType<HexGrid>();
            _hudManager = FindFirstObjectByType<HUDManager>();
            _gameManager = FindFirstObjectByType<GameManager>();
            _cardPlayService = FindFirstObjectByType<CardPlayService>();
        }

        /// <summary>
        /// Shows all valid targets for the currently selected card.
        /// </summary>
        public void ShowValidTargets(CardUI card)
        {
            if (card == null)
            {
                ClearHighlights();
                return;
            }

            ClearHighlights();

            CardSelectionManager.Instance?.EnterTargetSelection();

            if (_hexGrid == null)
            {
                _hexGrid = FindFirstObjectByType<HexGrid>();
            }

            if (_cardPlayService == null)
            {
                _cardPlayService = FindFirstObjectByType<CardPlayService>();
            }

            if (_hexGrid == null || _cardPlayService == null || card.runtimeCard == null)
            {
                _hudManager?.ShowError("Target selection is not ready.");
                return;
            }

            string actingPlayerKey = ResolveCurrentPlayerKey();
            //Ali changed this function, fixed coordinates.
            for (int row = 0; row < _hexGrid.gridHeight; row++)
            {
                for (int col = 0; col < _hexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    HexTile tile = _hexGrid.GetTile(coord);

                    if (tile == null)
                    {
                        continue;
                    }

                    //Ali : build the real target type from the clicked tile instead of always assuming Tile.
                    if (!TryBuildTargetFromTile(card.runtimeCard, tile, actingPlayerKey, out CardTarget target))
                    {
                        continue;
                    }


                    // Rabie: highlight only targets that the real card play pipeline says are legal.
                    CardPlayResult result = _cardPlayService.CanPlayCard(card.runtimeCard, actingPlayerKey, target);
                    if (result.Succeeded)
                    {
                        tile.Highlight(new Color(0.2f, 1f, 0.2f, 1f)); // Green
                        _highlightedTiles.Add(tile);
                    }
                }
            }

            Debug.Log($"[TargetSelectionManager] Showing {_highlightedTiles.Count} valid targets for {card.CardName}");
        }

        /// <summary>
        /// Clears all target highlights.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var tile in _highlightedTiles)
            {
                if (tile != null)
                {
                    tile.ResetColor();
                }
            }

            _highlightedTiles.Clear();
        }

        /// <summary>
        /// Processes a tile click as a target selection.
        /// </summary>
        public bool TrySelectTarget(HexTile targetTile)
        {
            CardSelectionManager selectionMgr = CardSelectionManager.Instance;
            if (selectionMgr?.SelectedCard == null)
            {
                return false;
            }
            CardUI selectedCard = selectionMgr.SelectedCard;


            if (!_highlightedTiles.Contains(targetTile))
            {
                if (_hudManager != null)
                {
                    _hudManager.ShowError("Invalid target selected.");
                }

                Debug.Log("[TargetSelectionManager] Target not in valid targets list.");
                return false;
            }

            string actingPlayerKey = ResolveCurrentPlayerKey();
            //Ali : rebuild the exact target from the clicked tile so spells can target units or forts.
            if (!TryBuildTargetFromTile(selectedCard.runtimeCard, targetTile, actingPlayerKey, out CardTarget target))
            {
                _hudManager?.ShowError("Could not build target from clicked tile.");
                return false;
            }


            selectionMgr.ConfirmSelection(target);
            // Rabie: send the confirmed tile to PlayerInputController so the selected card is actually played.
            PlayerInputController.Instance?.OnTargetConfirmed(target);

            Debug.Log($"[TargetSelectionManager] Target confirmed at ({targetTile.coord.q}, {targetTile.coord.r})");

            ClearHighlights();

            return true;
        }

        /// <summary>
        /// Called when selection is cancelled to reset highlights.
        /// </summary>
        public void OnSelectionCancelled()
        {
            ClearHighlights();
        }



        //Ali:
        private bool TryBuildTargetFromTile(CardRuntimeState sourceCard, HexTile tile, string actingPlayerKey, out CardTarget target)
        {
            //Ali : converts one clicked board tile into the target shape expected by the card pipeline.
            target = default;

            if (sourceCard == null || sourceCard.SourceCard == null || tile == null)
            {
                return false;
            }

            AxialCoord coord = new AxialCoord(tile.coord.q, tile.coord.r);

            if (sourceCard.SourceCard is CharacterCardData || sourceCard.SourceCard is WorldEffectCardData)
            {
                //Ali : board-placement cards still target an empty tile directly.
                target = new CardTarget
                {
                    type = CardTargetType.Tile,
                    tile = coord
                };
                return true;
            }

            if (sourceCard.SourceCard is SpellCardData)
            {
                if (tile.tileType == "fort")
                {
                    //Ali : spells can target allied or enemy forts, so we encode fort ownership here.
                    bool isAllyFort = tile.owner == actingPlayerKey;

                    target = new CardTarget
                    {
                        type = isAllyFort ? CardTargetType.AllyFort : CardTargetType.EnemyFort,
                        tile = coord,
                        targetPlayerId = tile.owner,
                        targetEntityId = "fort"
                    };
                    return true;
                }

                CardRuntimeState runtimeTarget = GetRuntimeCardOnTile(coord);
                if (runtimeTarget != null)
                {
                    //Ali : spells that hit units need the runtime card reference plus ally/enemy ownership.
                    bool isAllyUnit = tile.owner == actingPlayerKey;

                    target = new CardTarget
                    {
                        type = isAllyUnit ? CardTargetType.AllyUnit : CardTargetType.EnemyUnit,
                        tile = coord,
                        targetCard = runtimeTarget,
                        targetPlayerId = tile.owner
                    };
                    return true;
                }
            }

            return false;
        }

        private CardRuntimeState GetRuntimeCardOnTile(AxialCoord coord)
        {
            //Ali : ask the board reader for the manifested runtime card on this tile.
            return new HexGridBoardStateReader(_hexGrid).GetCardAt(coord);
        }

        private string ResolveCurrentPlayerKey()
        {
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_gameManager == null || _gameManager.currentPlayer == null)
            {
                return string.Empty;
            }

            if (ReferenceEquals(_gameManager.currentPlayer, _gameManager.player1))
            {
                return PlayerKeyResolver.PlayerOneKey;
            }

            if (ReferenceEquals(_gameManager.currentPlayer, _gameManager.player2))
            {
                return PlayerKeyResolver.PlayerTwoKey;
            }

            return _gameManager.currentPlayer.playerName;
        }
    }
}
