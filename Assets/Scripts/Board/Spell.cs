using UnityEngine;

public class Spell : MonoBehaviour
{
    public string owner;
    public CardRuntimeState sourceCard;
    public CardTarget target;
    public SpellEffectType effectType;
    public int effectPower;
    public int remainingDurationTurns;
    public bool isResolved;

    public void Initialize(string spellOwner, CardRuntimeState card, CardTarget spellTarget)
    {
        owner = string.IsNullOrWhiteSpace(spellOwner) ? "none" : spellOwner;
        sourceCard = card;
        target = spellTarget;
        effectType = card != null ? card.SpellEffectType : default;

        if (card != null && card.SpellEffectPower.HasValue)
        {
            effectPower = card.SpellEffectPower.Value;
        }
        else
        {
            effectPower = 0;
        }

        remainingDurationTurns = card != null ? Mathf.Max(0, card.RemainingEffectDurationTurns) : 0;
        isResolved = false;
    }

    public void MarkResolved()
    {
        isResolved = true;
    }

    public bool ConsumeDurationTurn()
    {
        if (remainingDurationTurns <= 0)
        {
            return true;
        }

        remainingDurationTurns = Mathf.Max(0, remainingDurationTurns - 1);
        return remainingDurationTurns <= 0;
    }

    public void Remove()
    {
        sourceCard = null;
        Destroy(gameObject);
    }
}
