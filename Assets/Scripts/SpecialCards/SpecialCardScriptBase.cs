public abstract class SpecialCardScriptBase : ISpecialCardScript
{
    public abstract bool IsMatch(Unit unit, CharacterCardData unitCardData);

    public virtual int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        return unit != null ? unit.attackRange : 0;
    }

    public virtual bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return false;
    }

    public virtual bool TryHandleAttack(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return false;
    }

    public virtual bool ConsumeMoveAction(Unit unit, CharacterCardData unitCardData)
    {
        return true;
    }

    public virtual void OnBeforeMove(Unit unit, CharacterCardData unitCardData)
    {
    }

    public virtual void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
    }

    protected static bool CardNameMatches(CharacterCardData cardData, string expected)
    {
        if (cardData == null || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        return cardData.DisplayName.Trim().ToLowerInvariant() == expected.Trim().ToLowerInvariant();
    }
}
