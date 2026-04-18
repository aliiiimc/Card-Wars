namespace FortGame.Computer
{
    /// <summary>
    /// Baseline validator used by the AI legal-action reader until full validator routing is available.
    /// </summary>
    public sealed class BasicComputerTargetValidator : ICardTargetValidator
    {
        public string ValidatorId => "computer.basic";

        public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null)
            {
                return CardValidationResult.Invalid("NO_CONTEXT", "Validation context is missing.");
            }

            if (sourceCard?.SourceCard == null)
            {
                return CardValidationResult.Invalid("NO_CARD", "Source card is missing.");
            }

            if (context.Board == null)
            {
                return CardValidationResult.Invalid("NO_BOARD", "Board reader is missing.");
            }

            switch (target.type)
            {
                case CardTargetType.Tile:
                    if (!context.Board.IsTileValid(target.tile))
                    {
                        return CardValidationResult.Invalid("INVALID_TILE", "Target tile does not exist.");
                    }

                    if (context.Board.IsTileOccupied(target.tile))
                    {
                        return CardValidationResult.Invalid("OCCUPIED_TILE", "Target tile is occupied.");
                    }

                    return CardValidationResult.Valid();

                case CardTargetType.EnemyFort:
                    if (string.IsNullOrWhiteSpace(target.targetPlayerId))
                    {
                        return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Enemy fort target player is missing.");
                    }

                    if (target.targetPlayerId != context.OpponentPlayerKey)
                    {
                        return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target fort does not belong to opponent.");
                    }

                    return CardValidationResult.Valid();

                default:
                    return CardValidationResult.Invalid("UNSUPPORTED_TARGET", "This validator only supports tile and enemy fort targeting.");
            }
        }
    }
}
