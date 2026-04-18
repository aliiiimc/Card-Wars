using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Public unified interface for legal-action queries used by both AI and player UI.
    /// Ensures both systems use the exact same legality rules.
    /// </summary>
    public sealed class LegalActionService : MonoBehaviour
    {
        private static LegalActionService _instance;

        private LegalActionGenerator _generator;
        private ComputerGameSnapshotProvider _snapshotProvider;

        public static LegalActionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LegalActionService>();
                    if (_instance == null)
                    {
                        GameObject serviceObj = new GameObject("LegalActionService");
                        _instance = serviceObj.AddComponent<LegalActionService>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _generator = new LegalActionGenerator(new BasicComputerTargetValidator());
            _snapshotProvider = new ComputerGameSnapshotProvider();
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Returns all legal actions for a given player in the current game state.
        /// Used by both AI and UI.
        /// </summary>
        public List<ComputerAction> GetLegalActions(ComputerPlayer player)
        {
            if (player == null)
            {
                return new List<ComputerAction>();
            }

            ComputerGameSnapshot snapshot = _snapshotProvider.CreateSnapshot(player);
            if (snapshot == null)
            {
                return new List<ComputerAction>();
            }

            return _generator.GenerateLegalActions(snapshot);
        }

        /// <summary>
        /// Checks if a specific action is currently legal.
        /// </summary>
        public bool IsActionLegal(ComputerPlayer player, ComputerAction action)
        {
            if (player == null || action == null)
            {
                return false;
            }

            List<ComputerAction> legal = GetLegalActions(player);
            for (int i = 0; i < legal.Count; i++)
            {
                if (legal[i] == action)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns diagnostics from the last legal-action generation.
        /// </summary>
        public LegalActionDiagnostics GetLastDiagnostics()
        {
            return _generator?.Diagnostics;
        }

        /// <summary>
        /// Logs a diagnostic summary of the last legal-action generation.
        /// </summary>
        public void LogLastDiagnostics(bool detailed = false)
        {
            LegalActionDiagnostics diagnostics = GetLastDiagnostics();
            if (diagnostics == null)
            {
                Debug.Log("[LegalActionService] No diagnostics available.");
                return;
            }

            if (detailed)
            {
                diagnostics.LogDetailed();
            }
            else
            {
                diagnostics.LogSummary();
            }
        }
    }
}
