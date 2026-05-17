using UnityEngine;

public class Miner : SpecialCardScriptBase
{
    private const float MovingVisibilityAlpha = 0.3f;

    public override bool IsMatch(Unit unit, CharacterCardData unitCardData)
    {
        return CardNameMatches(unitCardData, "Miner");
    }

    public override bool ConsumeMoveAction(Unit unit, CharacterCardData unitCardData)
    {
        return false;
    }

    public override void OnBeforeMove(Unit unit, CharacterCardData unitCardData)
    {
        SetAlpha(unit, MovingVisibilityAlpha);
    }

    public override void OnAfterMove(Unit unit, CharacterCardData unitCardData, HexTile destinationTile)
    {
        SetAlpha(unit, 1f);
    }

    private static void SetAlpha(Unit unit, float alpha)
    {
        if (unit == null)
        {
            return;
        }

        SpriteRenderer renderer = unit.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }
}
