using UnityEngine;

public class WorldEffect : MonoBehaviour
{
    public string owner;
    public HexTile currentTile;
    public CardRuntimeState sourceCard;
    public int health;
    public OptionalInt damage;
    public OptionalInt revenuePerTurn;
    public string effectName = "";

    public void PlaceOnTile(HexTile tile, string effectOwner, Sprite manifestedSprite = null)
    {
        if (tile == null)
        {
            return;
        }

        owner = string.IsNullOrWhiteSpace(effectOwner) ? "none" : effectOwner;
        currentTile = tile;
        tile.PlaceWorldEffect(owner, manifestedSprite);
        transform.position = tile.transform.position;
    }

    public void InitializeFromCard(CardRuntimeState card)
    {
        sourceCard = card;

        if (card == null || card.SourceCard == null)
        {
            return;
        }

        effectName = card.SourceCard.DisplayName;

        if (card.CurrentHp.HasValue)
        {
            health = card.CurrentHp.Value;
        }

        revenuePerTurn = card.CurrentRevenue;
    }

    public void RemoveFromBoard()
    {
        if (currentTile != null && currentTile.tileType == "worldEffect")
        {
            currentTile.RemoveUnit();
        }

        currentTile = null;
        Destroy(gameObject);
    }
}
