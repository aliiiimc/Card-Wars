public class Archer : SpecialCardScriptBase
{
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

        int bonusAttackRange = 2;
        if (unitCardData is ArcherCardData archerCardData)
        {
            bonusAttackRange = UnityEngine.Mathf.Max(0, archerCardData.bonusAttackRange);
        }

        return unit.attackRange + bonusAttackRange;
    }

    public override bool CanTarget(Unit attacker, CharacterCardData attackerCardData, HexTile tile, string activeOwner)
    {
        return tile != null
            && tile.owner != "none"
            && tile.owner != activeOwner
            && (tile.tileType == "unit" || tile.tileType == "worldEffect");
    }
}
