using UnityEngine;

public class Unit : MonoBehaviour
{
    public int moveRange = 2;
    public int attackRange = 1;
    public int health = 10;
    public int attack = 3;
    public string owner = "player";
    public HexTile currentTile;

    public void PlaceOnTile(HexTile tile)
    {
        currentTile = tile;
        tile.tileType = "unit";
        tile.owner = owner;
        transform.position = tile.transform.position;
    }

    public void Die()
    {
        if (currentTile != null)
        {
            currentTile.RemoveUnit();
            currentTile = null;
        }
        Destroy(gameObject);
    }
}