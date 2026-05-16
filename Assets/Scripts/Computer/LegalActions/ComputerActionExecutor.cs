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
                Debug.LogWarning($"[ComputerActionExecutor] Unsupported action type: {action.type}. Only card-play actions are handled here.");
                return false;
            }

            // Ali: card-play actions need a source card before calling CardPlayService.
            if (action.sourceCard == null)
            {
                Debug.LogWarning($"[ComputerActionExecutor] Invalid card action payload: {action.actionName}");
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


        // Ali: finds the shared CardPlayService used by both player and AI card play.
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


        // Ali: this executor currently supports only card-play actions.
        // Movement and attacks should use separate executors later.
        private static bool IsCardPlayAction(ActionType actionType)
        {
            return actionType == ActionType.PlayUnitCard
                || actionType == ActionType.PlayWorldEffectCard
                || actionType == ActionType.PlaySpellCard;
        }


    }
}
