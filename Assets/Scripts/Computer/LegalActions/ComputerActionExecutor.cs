using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Applies the selected legal action to current game state.
    /// </summary>
    public sealed class ComputerActionExecutor
    {
        // Ali: Executes the AI chosen action through CardPlayService so AI and player card rules stay identical.
        public bool TryExecuteAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (action == null || snapshot == null || snapshot.ActingPlayer == null)
            {
                return false;
            }

            if (action.endsTurn || action.type == ActionType.EndTurn)
            {
                return false;
            }

            if (!IsCardPlayAction(action.type))
            {
                Debug.LogWarning($"[ComputerActionExecutor] Unsupported action type: {action.type}");
                return false;
            }

            CardPlayService cardPlayService = ResolveCardPlayService(snapshot);
            if (cardPlayService == null)
            {
                Debug.LogWarning("[ComputerActionExecutor] Missing CardPlayService. AI card play was blocked to avoid rule drift.");
                return false;
            }

            string actingPlayerId = string.IsNullOrWhiteSpace(action.actingPlayerId)
                ? snapshot.ActingPlayerKey
                : action.actingPlayerId;

            CardPlayResult playResult = cardPlayService.PlayCard(action.sourceCard, actingPlayerId, action.target);
            if (!playResult.Succeeded)
            {
                Debug.LogWarning($"[ComputerActionExecutor] CardPlayService failed {action.actionName}: {playResult.ReasonCode} - {playResult.Message}");
                return false;
            }

            return true;
        }


        //Ali : AI can find the same CardPlayService used by player card play with this.
        private static CardPlayService ResolveCardPlayService(ComputerGameSnapshot snapshot)
        {
            if (snapshot?.GameManager != null)
            {
                CardPlayService serviceOnGameManager = snapshot.GameManager.GetComponent<CardPlayService>();
                if (serviceOnGameManager != null)
                {
                    return serviceOnGameManager;
                }
            }

            return Object.FindFirstObjectByType<CardPlayService>();
        }

        //Ali : Only card-play actions can safely use CardPlayService; other AI actions stay on their own execution path.
        private static bool IsCardPlayAction(ActionType actionType)
        {
            return actionType == ActionType.PlayUnitCard
                || actionType == ActionType.PlayWorldEffectCard
                || actionType == ActionType.PlaySpellCard;
        }

        private static bool ExecutePlacementAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            HexTile tile = snapshot.HexGrid.GetTile(action.target.tile);
            if (tile == null || !tile.IsEmpty())
            {
                return false;
            }

            if (action.type == ActionType.PlayWorldEffectCard)
            {
                if (!(action.sourceCard?.SourceCard is WorldEffectCardData worldEffectCard))
                {
                    return false;
                }

                // Rabie: world effects mark the tile as an effect, not as a normal unit.
                tile.PlaceWorldEffect(snapshot.ActingPlayerKey, worldEffectCard.manifestedSprite);
                action.sourceCard?.ManifestOnBoard(action.target.tile);
                return true;
            }

            if (!(action.sourceCard?.SourceCard is CharacterCardData))
            {
                return false;
            }

            Unit spawnedUnit = snapshot.HexGrid.SpawnUnitFromCard(tile, snapshot.ActingPlayerKey, action.sourceCard);
            if (spawnedUnit == null)
            {
                return false;
            }

            action.sourceCard?.ManifestOnBoard(action.target.tile);
            return true;
        }

        private static bool ExecuteSpellAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            if (snapshot.GameManager == null)
            {
                return false;
            }

            if (action.target.type == CardTargetType.EnemyFort)
            {
                if (ReferenceEquals(snapshot.OpponentPlayer, snapshot.GameManager.player1))
                {
                    snapshot.GameManager.DamagePlayer1Fort(1);
                    return true;
                }

                if (ReferenceEquals(snapshot.OpponentPlayer, snapshot.GameManager.player2))
                {
                    snapshot.GameManager.DamagePlayer2Fort(1);
                    return true;
                }
            }

            return false;
        }
    }
}
