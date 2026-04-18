using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer 
{
    /// <summary>
    /// The logic center for the ComputerPlayer. It evaluates the current game state
    /// and decides the best available action.
    /// </summary>
    public class ComputerBrain
    {
        private readonly ActionScoringSystem _scoringSystem;
        private readonly ComputerGameSnapshotProvider _snapshotProvider;
        private readonly LegalActionGenerator _legalActionGenerator;
        private readonly ComputerActionExecutor _actionExecutor;

        public ComputerBrain()
        {
            _scoringSystem = new ActionScoringSystem();
            _snapshotProvider = new ComputerGameSnapshotProvider();
            _legalActionGenerator = new LegalActionGenerator(new BasicComputerTargetValidator());
            _actionExecutor = new ComputerActionExecutor();
        }

        /// <summary>
        /// Analyzes the board and the AI's internal state to find the next move.
        /// Returns true if an action was chosen and executed; false if no actions are left.
        /// </summary>
        /// <param name="computer">The AI component owning this brain.</param>
        public bool DetermineNextAction(ComputerPlayer computer)
        {
            if (computer == null || computer.playerState == null)
            {
                return false;
            }

            Debug.Log($"[ComputerBrain] Evaluating actions for {computer.playerState.playerName} - Money Left: {computer.playerState.money}");

            ComputerGameSnapshot snapshot = _snapshotProvider.CreateSnapshot(computer);
            if (snapshot == null)
            {
                Debug.LogWarning("[ComputerBrain] Snapshot creation failed. Ending turn safely.");
                return false;
            }

            // 1. Gather all legally possible actions.
            List<ComputerAction> possibleActions = GeneratePossibleActions(snapshot);

            Debug.Log($"[ComputerBrain] Legal actions found: {possibleActions.Count}");

            if (_legalActionGenerator.Diagnostics != null)
            {
                _legalActionGenerator.Diagnostics.LogSummary();
            }

            // If we have no money or no possible moves, we can't do anything.
            if (possibleActions.Count == 0)
            {
                return false;
            }

            // 2. Score the actions using the Utility AI system
            ComputerAction bestAction = _scoringSystem.GetBestAction(possibleActions, snapshot.ActingPlayer, snapshot.CurrentTurn);

            if (bestAction != null)
            {
                if (bestAction.endsTurn || bestAction.type == ActionType.EndTurn)
                {
                    Debug.Log("[ComputerBrain] Best action is End Turn.");
                    return false;
                }

                // 3. Execute best action
                ExecuteAction(bestAction, snapshot);
                return true;
            }

            return false;
        }

        private List<ComputerAction> GeneratePossibleActions(ComputerGameSnapshot snapshot)
        {
            return _legalActionGenerator.GenerateLegalActions(snapshot);
        }

        private void ExecuteAction(ComputerAction action, ComputerGameSnapshot snapshot)
        {
            Debug.Log($"[ComputerBrain] Chose to execute: {action.actionName}");

            bool success = _actionExecutor.TryExecuteAction(action, snapshot);
            if (!success)
            {
                Debug.LogWarning($"[ComputerBrain] Failed to execute action: {action.actionName}");
            }
        }
    }
}
