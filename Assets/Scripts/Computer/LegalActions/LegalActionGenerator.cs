using System.Collections.Generic;

namespace FortGame.Computer
{
    /// <summary>
    /// Generates legal actions from a real game snapshot without mutating state.
    /// </summary>
    public sealed class LegalActionGenerator
    {
        private readonly ICardTargetValidator _targetValidator;
        private readonly LegalActionDiagnostics _diagnostics;

        public LegalActionGenerator(ICardTargetValidator targetValidator)
        {
            _targetValidator = targetValidator;
            _diagnostics = new LegalActionDiagnostics();
        }

        public LegalActionDiagnostics Diagnostics => _diagnostics;

        public List<ComputerAction> GenerateLegalActions(ComputerGameSnapshot snapshot)
        {
            List<ComputerAction> legalActions = new List<ComputerAction>();

            if (snapshot == null || snapshot.ActingPlayer == null || snapshot.HexGrid == null)
            {
                return legalActions;
            }

            _diagnostics.Reset();

            if (snapshot.CurrentPhase != GamePhase.Play)
            {
                legalActions.Add(ComputerAction.CreateEndTurnAction(snapshot.ActingPlayerKey));
                return legalActions;
            }

            CardValidationContext validationContext = new CardValidationContext
            {
                ActingPlayerKey = snapshot.ActingPlayerKey,
                OpponentPlayerKey = snapshot.OpponentPlayerKey,
                Board = snapshot.BoardReader
            };

            //Ali : évite une erreur si la main IA est absente/vide
            IReadOnlyList<CardRuntimeState> handCards = snapshot.HandCards;
            if (handCards == null || handCards.Count == 0)
            {
                legalActions.Add(ComputerAction.CreateEndTurnAction(snapshot.ActingPlayerKey));
                return legalActions;
            }


            for (int i = 0; i < handCards.Count; i++)
            {
                CardRuntimeState runtimeCard = handCards[i];
                if (runtimeCard?.SourceCard == null)
                {
                    _diagnostics.RecordCandidate();
                    continue;
                }

                if (runtimeCard.SourceCard is SpellCardData)
                {
                    TryAddSpellFortAction(legalActions, validationContext, snapshot, runtimeCard);
                    continue;
                }

                GenerateTilePlacementActions(legalActions, validationContext, snapshot, runtimeCard);
            }

            if (legalActions.Count == 0)
            {
                legalActions.Add(ComputerAction.CreateEndTurnAction(snapshot.ActingPlayerKey));
            }

            return legalActions;
        }

        private void TryAddSpellFortAction(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard)
        {
            if (snapshot == null || runtimeCard?.SourceCard == null)
            {
                return;
            }

            if (legalActions == null || validationContext == null)
            {
                return;
            }

            CardTarget target = new CardTarget
            {
                type = CardTargetType.EnemyFort,
                targetPlayerId = snapshot.OpponentPlayerKey,
                targetEntityId = "fort"
            };

            CardValidationResult result = _targetValidator.Validate(validationContext, runtimeCard, target);
            if (!result.IsValid)
            {
                _diagnostics.RecordCandidate();
                _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                return;
            }

            _diagnostics.RecordCandidate();
            var action = new ComputerAction($"Play {runtimeCard.SourceCard.DisplayName} on enemy fort", ActionType.PlaySpellCard)
            {
                actingPlayerId = snapshot.ActingPlayerKey,
                sourceCard = runtimeCard,
                sourceCardName = runtimeCard.SourceCard.DisplayName,
                target = target,
                cost = 0, // Ali: playing a card is free; card cost is only used when buying.
                isGeneratedByLegalReader = true,
                isLegalAction = true,
                willDestroyEnemyFort = WouldDestroyEnemyFort(snapshot, runtimeCard),
                isDefensiveMove = false,
                isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
            };

            legalActions.Add(action);
        }

        private void GenerateTilePlacementActions(
            List<ComputerAction> legalActions,
            CardValidationContext validationContext,
            ComputerGameSnapshot snapshot,
            CardRuntimeState runtimeCard)
        {
            if (snapshot?.HexGrid == null)
            {
                return;
            }
            if (legalActions == null || validationContext == null || runtimeCard?.SourceCard == null)
            {
                return;
            }


            for (int row = 0; row < snapshot.HexGrid.gridHeight; row++)
            {
                for (int col = 0; col < snapshot.HexGrid.gridWidth; col++)
                {
                    AxialCoord coord = HexGrid.OffsetToAxial(col, row);
                    CardTarget target = new CardTarget
                    {
                        type = CardTargetType.Tile,
                        tile = coord
                    };

                    CardValidationResult result = _targetValidator.Validate(validationContext, runtimeCard, target);
                    if (!result.IsValid)
                    {
                        _diagnostics.RecordCandidate();
                        _diagnostics.RecordRejection(runtimeCard.SourceCard.DisplayName, target, result.ReasonCode, result.Message);
                        continue;
                    }

                    _diagnostics.RecordCandidate();

                    ActionType actionType = runtimeCard.SourceCard is CharacterCardData
                        ? ActionType.PlayUnitCard
                        : ActionType.PlayWorldEffectCard;

                    var action = new ComputerAction($"Play {runtimeCard.SourceCard.DisplayName} on {coord}", actionType)
                    {
                        actingPlayerId = snapshot.ActingPlayerKey,
                        sourceCard = runtimeCard,
                        sourceCardName = runtimeCard.SourceCard.DisplayName,
                        target = target,
                        cost = 0, // Ali: playing a card is free; card cost is only used when buying.
                        isGeneratedByLegalReader = true,
                        isLegalAction = true,
                        isDefensiveMove = IsDefensiveColumn(snapshot, col),
                        movesCloserToEnemyFort = IsForwardColumn(snapshot, col),
                        isLateGameCard = runtimeCard.SourceCard.cost >= 4,
                        isEarlyGameCard = runtimeCard.SourceCard.cost <= 2
                    };

                    legalActions.Add(action);
                }
            }
        }


        // Ali: lets the AI recognize direct lethal Fort damage before scoring actions.
        private static bool WouldDestroyEnemyFort(ComputerGameSnapshot snapshot, CardRuntimeState runtimeCard)
        {
            if (snapshot == null || snapshot.OpponentPlayer == null)
            {
                return false;
            }

            if (!(runtimeCard?.SourceCard is SpellCardData spellCard))
            {
                return false;
            }

            if (spellCard.effectType != SpellEffectType.Damage)
            {
                return false;
            }

            return spellCard.effectPower >= snapshot.OpponentPlayer.fortHp;
        }


        private static bool IsDefensiveColumn(ComputerGameSnapshot snapshot, int col)
        {
            // Rabie: horizontal board - player defends left, computer/enemy defends right.
            return snapshot.ActingPlayerKey == "enemy"
                ? col >= snapshot.HexGrid.gridWidth - 2
                : col <= 1;
        }

        private static bool IsForwardColumn(ComputerGameSnapshot snapshot, int col)
        {
            // Rabie: computer/red side moves left toward the player fort; player moves right.
            return snapshot.ActingPlayerKey == "enemy"
                ? col < snapshot.HexGrid.gridWidth - 2
                : col > 1;
        }
    }
}
