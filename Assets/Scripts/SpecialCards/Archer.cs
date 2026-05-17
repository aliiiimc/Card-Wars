public class Archer : SpecialCardScriptBase
{
    private const int BonusAttackRange = 2;

    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Archer");
    }

    public override int GetAttackRange(Unit unit, CharacterCardData unitCardData)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.attackRange + BonusAttackRange;
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return tile != null
            && tile.owner != "none"
            && tile.owner != activeOwner
            && (tile.tileType == "unit" || tile.tileType == "worldEffect");
    }
}
