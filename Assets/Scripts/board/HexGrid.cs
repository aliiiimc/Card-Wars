using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public GameObject hexPrefab;
    public int gridWidth = 7;
    public int gridHeight = 5;
    public float hexSize = 0.5f;

    private HexTile[,] tiles;

    void Start()
    {
        tiles = new HexTile[gridWidth, gridHeight];
        GenerateGrid();
        PlaceForts();
        SpawnTestUnit();
    }

    void GenerateGrid()
    {
        float hexWidth = Mathf.Sqrt(3f) * hexSize;
        float hexHeight = 2f * hexSize;
        int midRow = gridHeight / 2;

        for (int r = 0; r < gridHeight; r++)
        {
            for (int q = 0; q < gridWidth; q++)
            {
                float offset = (r % 2 == 1) ? hexWidth * 0.5f : 0f;
                float xPos = q * hexWidth + offset;
                float yPos = r * hexHeight * 0.75f;

                Vector3 position = new Vector3(xPos, yPos, 0f);
                GameObject hex = Instantiate(hexPrefab, position, Quaternion.identity, transform);
                hex.name = $"Hex_{q}_{r}";

                HexTile tile = hex.GetComponent<HexTile>();
                tile.q = q;
                tile.r = r;
                tiles[q, r] = tile;

                // Color by territory
                SpriteRenderer sr = hex.GetComponent<SpriteRenderer>();
                if (r < midRow)
                    sr.color = new Color(0.6f, 0.8f, 1f); // light blue (player)
                else if (r >= midRow)
                    sr.color = new Color(1f, 0.7f, 0.7f); // light red (enemy)
                else
                    sr.color = new Color(0.9f, 0.9f, 0.9f); // neutral middle row
            }
        }
    }

    void PlaceForts()
    {
        int midCol = gridWidth / 2;
        tiles[midCol, 0].SetAsFort(new Color(0.1f, 0.2f, 0.8f), "player");
        tiles[midCol, gridHeight - 1].SetAsFort(new Color(0.8f, 0.1f, 0.1f), "enemy");
    }
    public HexTile GetTile(int q, int r)
    {
        if (q >= 0 && q < gridWidth && r >= 0 && r < gridHeight)
            return tiles[q, r];
        return null;
    }
    public GameObject unitPrefab;

    void SpawnTestUnit()
    {
        HexTile tile = tiles[3, 2];
        GameObject unitObj = Instantiate(unitPrefab, tile.transform.position, Quaternion.identity);
        Unit unit = unitObj.GetComponent<Unit>();
        unit.owner = "player";
        unit.PlaceOnTile(tile);
    }
}
