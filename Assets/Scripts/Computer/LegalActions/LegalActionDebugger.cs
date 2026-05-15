using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Debug utility component for testing legal-action generation.
    /// Attach to a UI button or use from console to inspect legal actions.
    /// </summary>
    public sealed class LegalActionDebugger : MonoBehaviour
    {
        public ComputerPlayer targetPlayer;

        public void DebugLegalActionsForPlayer()
        {
            if (targetPlayer == null)
            {
                targetPlayer = FindFirstObjectByType<ComputerPlayer>();
            }

            if (targetPlayer == null)
            {
                Debug.LogError("[LegalActionDebugger] No ComputerPlayer found.");
                return;
            }

            Debug.Log($"[LegalActionDebugger] Checking legal actions for {targetPlayer.playerState.playerName}...");

            var actions = LegalActionService.Instance.GetLegalActions(targetPlayer);
            Debug.Log($"[LegalActionDebugger] Found {actions.Count} legal actions:");

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                Debug.Log($"  [{i}] {action.actionName} (play cost: {action.cost}, type: {action.type})");
            }

            LegalActionService.Instance.LogLastDiagnostics(detailed: false);
        }

        public void DebugLegalActionsDetailed()
        {
            if (targetPlayer == null)
            {
                targetPlayer = FindFirstObjectByType<ComputerPlayer>();
            }

            if (targetPlayer == null)
            {
                Debug.LogError("[LegalActionDebugger] No ComputerPlayer found.");
                return;
            }

            var actions = LegalActionService.Instance.GetLegalActions(targetPlayer);
            Debug.Log($"[LegalActionDebugger] Detailed analysis of {actions.Count} legal actions:");

            LegalActionService.Instance.LogLastDiagnostics(detailed: true);
        }
    }
}
