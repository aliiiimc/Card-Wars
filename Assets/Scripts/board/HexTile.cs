using UnityEngine;

public class HexTile : MonoBehaviour
{
    public AxialCoord coord;
    public string tileType = "empty"; // empty, fort, unit
    public string owner = "none";     // none, player, enemy

    private static readonly Color PlayerUnitTileColor = new Color(0.14f, 0.50f, 0.56f);
    private static readonly Color EnemyUnitTileColor = new Color(0.58f, 0.22f, 0.28f);

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer worldEffectRenderer;
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
        ClearWorldEffectVisual();
        originalColor = unitOwner == "player" ? PlayerUnitTileColor : EnemyUnitTileColor;
        spriteRenderer.color = originalColor;
    }

    public void PlaceWorldEffect(string effectOwner, Sprite effectSprite = null)
    {
        // Rabie: world effect cards reserve the tile without pretending to be units.
        tileType = "worldEffect";
        owner = effectOwner;

        if (effectSprite != null)
        {
            SetWorldEffectVisual(effectSprite);
            originalColor = baseColor;
            spriteRenderer.color = baseColor;
            return;
        }

        if (worldEffectRenderer == null || worldEffectRenderer.sprite == null)
        {
            originalColor = new Color(0.4f, 0.8f, 0.3f);
            spriteRenderer.color = originalColor;
        }
        else
        {
            originalColor = baseColor;
            spriteRenderer.color = baseColor;
        }
    }

    public void RemoveUnit()
    {
        tileType = "empty";
        owner = "none";
        ClearWorldEffectVisual();
        originalColor = baseColor;
        spriteRenderer.color = baseColor;
    }

    private void SetWorldEffectVisual(Sprite effectSprite)
    {
        if (worldEffectRenderer == null)
        {
            GameObject visualObject = new GameObject("WorldEffectVisual");
            visualObject.transform.SetParent(transform, false);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;
            visualObject.transform.localScale = Vector3.one;

            worldEffectRenderer = visualObject.AddComponent<SpriteRenderer>();
            worldEffectRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            worldEffectRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        worldEffectRenderer.sprite = effectSprite;
        worldEffectRenderer.enabled = true;
    }

    private void ClearWorldEffectVisual()
    {
        if (worldEffectRenderer == null)
        {
            return;
        }

        worldEffectRenderer.sprite = null;
        worldEffectRenderer.enabled = false;
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
