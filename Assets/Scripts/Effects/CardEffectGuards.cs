public static class CardEffectGuards
{
    public static bool TryRequireContextAndWriter(CardEffectContext context, out CardEffectResult failure)
    {
        if (context == null)
        {
            failure = CardEffectResult.Failure("NO_CONTEXT", "Effect context is missing.");
            return false;
        }

        if (context.Writer == null)
        {
            failure = CardEffectResult.Failure("NO_WRITER", "State writer is missing.");
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireSourceCard(CardRuntimeState sourceCard, out CardEffectResult failure)
    {
        if (sourceCard == null || sourceCard.SourceCard == null)
        {
            failure = CardEffectResult.Failure("NO_CARD", "Source card is missing.");
            return false;
        }

        failure = default;
        return true;
    }

    public static bool TryRequireTargetCard(CardTarget target, string effectLabel, out CardEffectResult failure)
    {
        if (target.targetCard == null)
        {
            failure = CardEffectResult.Failure("NO_TARGET_CARD", $"{effectLabel} effect needs a target card.");
            return false;
        }

        failure = default;
        return true;
    }
}
