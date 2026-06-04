using System.Collections.Generic;
using UnityEngine;

public sealed class Revival
{
    private const string CardName = "Revival";

    public bool IsMatch(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData))
        {
            return false;
        }

        return sourceCard.SourceCard.MatchesSpecialCard(SpecialCardIds.SpellRevival, CardName);
    }

    public int GetLookbackTurns(CardRuntimeState sourceCard)
    {
        if (!(sourceCard?.SourceCard is SpellCardData spellCard))
        {
            return 0;
        }

        return Mathf.Max(0, spellCard.effectDurationTurns);
    }

    public CardEffectResult ValidateChoiceWindow(CardRuntimeState sourceCard, List<CharacterCardData> choices)
    {
        int lookbackTurns = GetLookbackTurns(sourceCard);
        if (lookbackTurns <= 0)
        {
            return CardEffectResult.Failure("INVALID_LOOKBACK", "Revival needs effectDurationTurns above zero.");
        }

        if (choices == null || choices.Count == 0)
        {
            return CardEffectResult.Failure("NO_REVIVAL_TARGETS", "No recently defeated characters are available for Revival.");
        }

        return CardEffectResult.Success($"Choose one of {choices.Count} recently defeated character(s) to revive.");
    }
}
