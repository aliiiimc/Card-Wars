using UnityEngine;

public class HexTile : MonoBehaviour
{
    public AxialCoord coord;
    public string tileType = "empty"; // empty, fort, unit
    public string owner = "none";     // none, player, enemy

    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private Color originalColor;

    private bool isHighlighted; //Ali : variable bach nt7ekmo f selection dial cards 


    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        originalColor = spriteRenderer.color;
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;

        if (tileType == "empty")
        {
            originalColor = color;
            spriteRenderer.color = color;
        }
    }

    public bool IsEmpty()
    {
        return tileType == "empty";
    }

    public void SetAsFort(Color fortColor, string fortOwner)
    {
        tileType = "fort";
        owner = fortOwner;
        originalColor = fortColor;
        spriteRenderer.color = fortColor;
    }

    public void PlaceUnit(string unitOwner)
    {
        tileType = "unit";
        owner = unitOwner;
        originalColor = unitOwner == "player" ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.3f);
        spriteRenderer.color = originalColor;
    }

    public void PlaceWorldEffect(string effectOwner)
    {
        // Rabie: world effect cards reserve the tile without pretending to be units.
        tileType = "worldEffect";
        owner = effectOwner;
        originalColor = new Color(0.4f, 0.8f, 0.3f);
        spriteRenderer.color = originalColor;
    }

    public void RemoveUnit()
    {
        tileType = "empty";
        owner = "none";
        originalColor = baseColor;
        spriteRenderer.color = baseColor;
    }

    public void Highlight(Color color)
    {
        isHighlighted = true; //Ali
        spriteRenderer.color = color;
    }

    public void ResetColor()
    {
        isHighlighted = false; //Ali
        spriteRenderer.color = originalColor;
    }

    void OnMouseDown()
    {
        Debug.Log($"Clicked ({coord.q},{coord.r}) | Type: {tileType} | Owner: {owner}");

        FortGame.UI.TargetSelectionManager targetMgr = FortGame.UI.TargetSelectionManager.Instance;
        if (targetMgr != null)
        {
            targetMgr.TrySelectTarget(this);
        }
    }

    void OnMouseEnter()
    {
        if (isHighlighted)//Ali 
        {
            return;
        }

        if (tileType != "fort")
        {
            spriteRenderer.color = Color.cyan;
        }
    }

    void OnMouseExit()
    {
        if (isHighlighted) //Ali
        {
            return;
        }
        spriteRenderer.color = originalColor;
    }
}
