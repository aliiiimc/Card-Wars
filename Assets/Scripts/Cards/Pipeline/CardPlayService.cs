using UnityEngine;

public sealed class CardPlayService : MonoBehaviour, ICardPlayService
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private HexGrid boardSource;
    [SerializeField] private GameManagerCardStateWriter writer;
    [SerializeField] private bool autoResolveDependencies = true;

    private readonly ICardPlayPipeline pipeline = new CardPlayPipeline();
    private readonly ICardTargetValidator fallbackValidator = new FallbackTargetValidator();

    private void Awake()
    {
        if (!autoResolveDependencies)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (boardSource == null)
        {
            boardSource = FindFirstObjectByType<HexGrid>();
        }

        if (writer == null)
        {
            writer = FindFirstObjectByType<GameManagerCardStateWriter>();
        }
    }

    public CardPlayResult PlayCard(CardRuntimeState sourceCard, string actingPlayerId, CardTarget target)
    {
        if (gameManager == null)
        {
            return Fail("NO_GAME_MANAGER", "Game manager is missing.", sourceCard);
        }

        if (gameManager.currentPhase != GamePhase.Play)
        {
            return Fail("WRONG_PHASE", "Cards can only be played during Play phase.", sourceCard);
        }

        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            return Fail("NO_CARD", "Source card is missing.", sourceCard);
        }

        if (writer == null)
        {
            ResolveWriter();
            if (writer == null)
            {
                return Fail("NO_WRITER", "State writer is missing.", sourceCard);
            }
        }

        string resolvedActor = ResolveActingPlayerId(actingPlayerId);
        if (string.IsNullOrWhiteSpace(resolvedActor))
        {
            return Fail("NO_ACTOR", "Acting player id is missing.", sourceCard);
        }

        string opponentPlayerId = ResolveOpponentPlayerId(resolvedActor);
        if (string.IsNullOrWhiteSpace(opponentPlayerId))
        {
            return Fail("NO_OPPONENT", "Opponent player id could not be resolved.", sourceCard);
        }

        ICardTargetValidator validator = ResolveValidator(sourceCard.SourceCard);
        ICardEffect effect = ResolveEffect(sourceCard.SourceCard);
        IBoardStateReader boardReader = boardSource != null
            ? new FortGame.Computer.HexGridBoardStateReader(boardSource)
            : null;

        CardPlayRequest request = new CardPlayRequest
        {
            ActingPlayerId = resolvedActor,
            OpponentPlayerId = opponentPlayerId,
            SourceCard = sourceCard,
            Target = target,
            Board = boardReader,
            Writer = writer,
            Validator = validator,
            Effect = effect
        };

        CardPlayResult result = pipeline.Play(request);
        if (!result.Succeeded)
        {
            return result;
        }

        RemoveCardFromActingHand(sourceCard, resolvedActor);
        return result;
    }

    private static CardPlayResult Fail(string reasonCode, string message, CardRuntimeState sourceCard)
    {
        return CardPlayResult.Failure(
            reasonCode,
            message,
            CardValidationResult.Valid(),
            CardEffectResult.Success(),
            costWasSpent: false,
            finalZone: sourceCard != null ? sourceCard.CurrentZone : CardZone.Hand);
    }

    private string ResolveActingPlayerId(string requestedPlayerId)
    {
        if (string.IsNullOrWhiteSpace(requestedPlayerId))
        {
            return ResolveCurrentPlayerId();
        }

        if (MatchesPlayer(gameManager.player1, requestedPlayerId))
        {
            return "player";
        }

        if (MatchesPlayer(gameManager.player2, requestedPlayerId))
        {
            return "enemy";
        }

        return requestedPlayerId;
    }

    private string ResolveCurrentPlayerId()
    {
        if (gameManager.currentPlayer == null)
        {
            return string.Empty;
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player1))
        {
            return "player";
        }

        if (ReferenceEquals(gameManager.currentPlayer, gameManager.player2))
        {
            return "enemy";
        }

        return gameManager.currentPlayer.playerName;
    }

    private string ResolveOpponentPlayerId(string actingPlayerId)
    {
        if (actingPlayerId == "player")
        {
            return "enemy";
        }

        if (actingPlayerId == "enemy")
        {
            return "player";
        }

        if (MatchesPlayer(gameManager.player1, actingPlayerId))
        {
            return "enemy";
        }

        if (MatchesPlayer(gameManager.player2, actingPlayerId))
        {
            return "player";
        }

        return string.Empty;
    }

    private bool MatchesPlayer(PlayerState player, string playerId)
    {
        if (player == null || string.IsNullOrWhiteSpace(playerId))
        {
            return false;
        }

        if (ReferenceEquals(player, gameManager.player1))
        {
            return playerId == "player" || playerId == player.playerName;
        }

        if (ReferenceEquals(player, gameManager.player2))
        {
            return playerId == "enemy" || playerId == player.playerName;
        }

        return playerId == player.playerName;
    }

    private void RemoveCardFromActingHand(CardRuntimeState sourceCard, string actingPlayerId)
    {
        if (sourceCard == null || gameManager == null)
        {
            return;
        }

        PlayerState actingPlayer = ResolveActingPlayerState(actingPlayerId);
        if (actingPlayer == null || actingPlayer.handCards == null)
        {
            return;
        }

        if (actingPlayer.handCards.Remove(sourceCard))
        {
            actingPlayer.handCount = actingPlayer.handCards.Count;
        }
    }

    private PlayerState ResolveActingPlayerState(string actingPlayerId)
    {
        if (MatchesPlayer(gameManager.player1, actingPlayerId))
        {
            return gameManager.player1;
        }

        if (MatchesPlayer(gameManager.player2, actingPlayerId))
        {
            return gameManager.player2;
        }

        return null;
    }

    private void ResolveWriter()
    {
        if (writer != null)
        {
            return;
        }

        writer = FindFirstObjectByType<GameManagerCardStateWriter>();
        if (writer == null && gameManager != null)
        {
            writer = gameManager.GetComponent<GameManagerCardStateWriter>();
            if (writer == null)
            {
                writer = gameManager.gameObject.AddComponent<GameManagerCardStateWriter>();
            }
        }
    }

    private ICardTargetValidator ResolveValidator(CardData cardData)
    {
        string validatorId = cardData != null ? cardData.validatorId : string.Empty;
        ICardTargetValidator mapped = ResolveSceneComponentById<ICardTargetValidator>(
            validatorId,
            candidate => candidate.ValidatorId);
        return mapped ?? fallbackValidator;
    }

    private ICardEffect ResolveEffect(CardData cardData)
    {
        string effectId = cardData != null ? cardData.effectId : string.Empty;
        ICardEffect mapped = ResolveSceneComponentById<ICardEffect>(
            effectId,
            candidate => candidate.EffectId);
        return mapped ?? FallbackEffectFactory.Create(cardData);
    }

    private static T ResolveSceneComponentById<T>(string id, System.Func<T, string> getId) where T : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is T candidate && getId(candidate) == id)
            {
                return candidate;
            }
        }

        return null;
    }

    private sealed class FallbackTargetValidator : ICardTargetValidator
    {
        public string ValidatorId => "target.rules.fallback";

        public CardValidationResult Validate(CardValidationContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null)
            {
                return CardValidationResult.Invalid("NO_CONTEXT", "Validation context is missing.");
            }

            if (sourceCard == null || sourceCard.SourceCard == null)
            {
                return CardValidationResult.Invalid("NO_CARD", "Source card is missing.");
            }

            if (target.type == CardTargetType.Tile)
            {
                if (context.Board != null)
                {
                    if (!context.Board.IsTileValid(target.tile))
                    {
                        return CardValidationResult.Invalid("INVALID_TILE", "Tile is outside board bounds.");
                    }

                    if (context.Board.IsTileOccupied(target.tile))
                    {
                        return CardValidationResult.Invalid("TILE_OCCUPIED", "Tile is occupied.");
                    }
                }

                return CardValidationResult.Valid();
            }

            if (target.type == CardTargetType.EnemyFort)
            {
                if (string.IsNullOrWhiteSpace(target.targetPlayerId))
                {
                    return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Enemy fort target player is required.");
                }

                if (target.targetPlayerId != context.OpponentPlayerKey)
                {
                    return CardValidationResult.Invalid("WRONG_TARGET_PLAYER", "Target player is not the current opponent.");
                }

                return CardValidationResult.Valid();
            }

            if (target.type == CardTargetType.Player)
            {
                if (string.IsNullOrWhiteSpace(target.targetPlayerId))
                {
                    return CardValidationResult.Invalid("MISSING_TARGET_PLAYER", "Player target id is missing.");
                }

                return CardValidationResult.Valid();
            }

            return CardValidationResult.Invalid("UNSUPPORTED_TARGET", $"Target type '{target.type}' is not supported.");
        }
    }

    private static class FallbackEffectFactory
    {
        public static ICardEffect Create(CardData cardData)
        {
            if (cardData is CharacterCardData || cardData is WorldEffectCardData)
            {
                return new ManifestEffect();
            }

            if (cardData is SpellCardData spellCard)
            {
                switch (spellCard.effectType)
                {
                    case SpellEffectType.Damage:
                        return new SpellDamageEffect(spellCard.effectPower);
                    case SpellEffectType.Heal:
                        return new SpellHealEffect(spellCard.effectPower);
                    case SpellEffectType.Buff:
                        return new SpellBuffEffect(spellCard.effectPower);
                    case SpellEffectType.Debuff:
                        return new SpellDebuffEffect(spellCard.effectPower);
                    case SpellEffectType.Boost:
                        return new IncomeEffect(spellCard.effectPower);
                    case SpellEffectType.Summon:
                        return new ManifestEffect();
                }
            }

            return new UnsupportedEffect();
        }
    }

    private sealed class ManifestEffect : ICardEffect
    {
        public string EffectId => "effect.fallback.manifest";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null || context.Writer == null)
            {
                return CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
            }

            if (sourceCard == null || sourceCard.SourceCard == null)
            {
                return CardEffectResult.Failure("NO_CARD", "Source card is missing.");
            }

            if (target.type != CardTargetType.Tile)
            {
                return CardEffectResult.Failure("WRONG_TARGET", "Card requires a tile target.");
            }

            context.Writer.ManifestCard(sourceCard, target.tile);
            return CardEffectResult.Success("Card manifested.");
        }
    }

    private abstract class TargetCardEffectBase : ICardEffect
    {
        protected readonly int amount;

        protected TargetCardEffectBase(int amount)
        {
            this.amount = Mathf.Max(0, amount);
        }

        public abstract string EffectId { get; }
        protected abstract string MissingTargetMessage { get; }
        protected abstract CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard);

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null || context.Writer == null)
            {
                return CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
            }

            if (target.targetCard == null)
            {
                return CardEffectResult.Failure("NO_TARGET_CARD", MissingTargetMessage);
            }

            return ApplyToTarget(context.Writer, target.targetCard);
        }
    }

    private sealed class SpellDamageEffect : TargetCardEffectBase
    {
        public SpellDamageEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.damage";
        protected override string MissingTargetMessage => "Damage spell needs a target card.";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ApplyDamage(targetCard, amount);
            return CardEffectResult.Success("Damage applied.", damageDealt: amount);
        }
    }

    private sealed class SpellHealEffect : TargetCardEffectBase
    {
        public SpellHealEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.heal";
        protected override string MissingTargetMessage => "Heal spell needs a target card.";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ApplyHeal(targetCard, amount);
            return CardEffectResult.Success("Heal applied.", healApplied: amount);
        }
    }

    private sealed class SpellBuffEffect : TargetCardEffectBase
    {
        public SpellBuffEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.buff";
        protected override string MissingTargetMessage => "Buff spell needs a target card.";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ModifyDamage(targetCard, amount);
            writer.ModifyMovement(targetCard, amount);
            return CardEffectResult.Success("Buff applied.");
        }
    }

    private sealed class SpellDebuffEffect : TargetCardEffectBase
    {
        public SpellDebuffEffect(int amount)
            : base(amount)
        {
        }

        public override string EffectId => "effect.fallback.debuff";
        protected override string MissingTargetMessage => "Debuff spell needs a target card.";

        protected override CardEffectResult ApplyToTarget(ICardStateWriter writer, CardRuntimeState targetCard)
        {
            writer.ModifyDamage(targetCard, -amount);
            writer.ModifyMovement(targetCard, -amount);
            return CardEffectResult.Success("Debuff applied.");
        }
    }

    private sealed class IncomeEffect : ICardEffect
    {
        private readonly int amount;
        public IncomeEffect(int amount)
        {
            this.amount = Mathf.Max(0, amount);
        }

        public string EffectId => "effect.fallback.income";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            if (context == null || context.Writer == null)
            {
                return CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
            }

            if (string.IsNullOrWhiteSpace(context.ActingPlayerKey))
            {
                return CardEffectResult.Failure("NO_ACTOR", "Acting player id is missing.");
            }

            context.Writer.AddRevenue(context.ActingPlayerKey, amount);
            return CardEffectResult.Success("Income added.", revenueGained: amount);
        }
    }

    private sealed class UnsupportedEffect : ICardEffect
    {
        public string EffectId => "effect.fallback.unsupported";

        public CardEffectResult Apply(CardEffectContext context, CardRuntimeState sourceCard, CardTarget target)
        {
            return CardEffectResult.Failure("NO_EFFECT_MAPPING", "No effect mapping found for this card.");
        }
    }
}
