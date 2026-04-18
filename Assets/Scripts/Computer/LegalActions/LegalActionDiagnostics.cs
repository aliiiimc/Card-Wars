using System.Collections.Generic;
using UnityEngine;

namespace FortGame.Computer
{
    /// <summary>
    /// Tracks rejected action candidates and their failure reasons for debugging and analytics.
    /// </summary>
    public sealed class LegalActionDiagnostics
    {
        public struct RejectedAction
        {
            public string CardName { get; set; }
            public CardTarget Target { get; set; }
            public string ReasonCode { get; set; }
            public string Message { get; set; }
        }

        private readonly List<RejectedAction> _rejectedActions = new List<RejectedAction>();
        private int _totalCandidatesGenerated = 0;

        public int TotalCandidatesGenerated => _totalCandidatesGenerated;
        public int TotalRejected => _rejectedActions.Count;
        public IReadOnlyList<RejectedAction> RejectedActions => _rejectedActions.AsReadOnly();

        public void RecordCandidate()
        {
            _totalCandidatesGenerated++;
        }

        public void RecordRejection(string cardName, CardTarget target, string reasonCode, string message)
        {
            _rejectedActions.Add(new RejectedAction
            {
                CardName = cardName,
                Target = target,
                ReasonCode = reasonCode,
                Message = message
            });
        }

        public void Reset()
        {
            _rejectedActions.Clear();
            _totalCandidatesGenerated = 0;
        }

        public void LogSummary()
        {
            Debug.Log($"[LegalActionDiagnostics] Summary: {_totalCandidatesGenerated} candidates, {_rejectedActions.Count} rejected.");

            Dictionary<string, int> reasonCounts = new Dictionary<string, int>();
            foreach (var rejected in _rejectedActions)
            {
                if (!reasonCounts.ContainsKey(rejected.ReasonCode))
                {
                    reasonCounts[rejected.ReasonCode] = 0;
                }

                reasonCounts[rejected.ReasonCode]++;
            }

            foreach (var kvp in reasonCounts)
            {
                Debug.Log($"  [{kvp.Key}] {kvp.Value} rejections");
            }
        }

        public void LogDetailed()
        {
            LogSummary();

            if (_rejectedActions.Count > 0)
            {
                Debug.Log("[LegalActionDiagnostics] Detailed rejections:");
                for (int i = 0; i < _rejectedActions.Count && i < 10; i++)
                {
                    var rejected = _rejectedActions[i];
                    Debug.Log($"  {rejected.CardName} on {rejected.Target.tile} -> {rejected.ReasonCode}: {rejected.Message}");
                }

                if (_rejectedActions.Count > 10)
                {
                    Debug.Log($"  ... and {_rejectedActions.Count - 10} more.");
                }
            }
        }
    }
}
