using UnityEngine;

public class HexTile : MonoBehaviour
{
    public AxialCoord coord;
    public string tileType = "empty"; // empty, fort, unit
    public string owner = "none";     // none, player, enemy
    public bool hasWorldEffect;
    public string worldEffectOwner = "none";
    public bool worldEffectAllowsUnitPassThrough;
    public bool worldEffectAllowsUnitOccupancy;
    public bool isFieldTile;
    public string fieldClusterId = "";
    public int fieldHp;
    public int fieldBonusMoneyPerTurn;
    public bool isMineTile;
    public int mineDamage;
    public bool isCampTile;

    private static readonly Color PlayerUnitTileColor = new Color(0.14f, 0.50f, 0.56f);
    private static readonly Color EnemyUnitTileColor = new Color(0.58f, 0.22f, 0.28f);

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer fortRenderer;
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

        if (tileType == "empty" || tileType == "worldEffect")
        {
            originalColor = color;
            spriteRenderer.color = color;
        }
    }

    public bool IsEmpty()
    {
        return tileType == "empty" && (!hasWorldEffect || worldEffectAllowsUnitOccupancy);
    }

    public bool HasUnitOccupant()
    {
        return tileType == "unit";
    }

    public bool HasWorldEffect()
    {
        return hasWorldEffect;
    }

    public bool CanPlaceWorldEffect()
    {
        return tileType == "empty" && !hasWorldEffect;
    }

    public bool CanUnitPassThrough()
    {
        if (tileType == "fort" || tileType == "unit")
        {
            return false;
        }

        return !hasWorldEffect || worldEffectAllowsUnitPassThrough;
    }

    public bool CanUnitOccupy()
    {
        if (tileType == "fort" || tileType == "unit")
        {
            return false;
        }

        return !hasWorldEffect || worldEffectAllowsUnitOccupancy;
    }

    public void SetAsFort(Color fortColor, string fortOwner, Sprite fortSprite = null)
    {
        tileType = "fort";
        owner = fortOwner;
        ClearFieldData();
        originalColor = fortColor;
        spriteRenderer.color = fortColor;

        if (fortSprite != null)
        {
            SetFortVisual(fortSprite);
        }
        else
        {
            ClearFortVisual();
        }
    }

    public void PlaceUnit(string unitOwner)
    {
        tileType = "unit";
        owner = unitOwner;
        originalColor = unitOwner == "player" ? PlayerUnitTileColor : EnemyUnitTileColor;
        spriteRenderer.color = originalColor;
    }

    public void PlaceWorldEffect(
        string effectOwner,
        Sprite effectSprite = null,
        bool allowsUnitPassThrough = false,
        bool allowsUnitOccupancy = false,
        float opacity = 1f)
    {
        hasWorldEffect = true;
        worldEffectOwner = string.IsNullOrWhiteSpace(effectOwner) ? "none" : effectOwner;
        worldEffectAllowsUnitPassThrough = allowsUnitPassThrough;
        worldEffectAllowsUnitOccupancy = allowsUnitOccupancy;

        if (!HasUnitOccupant())
        {
            if (worldEffectAllowsUnitOccupancy)
            {
                tileType = "empty";
                owner = "none";
            }
            else
            {
                tileType = "worldEffect";
                owner = worldEffectOwner;
            }
        }

        if (effectSprite != null)
        {
            SetWorldEffectVisual(effectSprite, opacity);
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

    public void SetWorldEffectOwner(string effectOwner)
    {
        if (!hasWorldEffect)
        {
            return;
        }

        worldEffectOwner = string.IsNullOrWhiteSpace(effectOwner) ? "none" : effectOwner;
        if (tileType == "worldEffect")
        {
            owner = worldEffectOwner;
        }
    }

    public void ClearUnitOccupant()
    {
        if (tileType != "unit")
        {
            return;
        }

        if (hasWorldEffect)
        {
            if (worldEffectAllowsUnitOccupancy)
            {
                tileType = "empty";
                owner = "none";
            }
            else
            {
                tileType = "worldEffect";
                owner = worldEffectOwner;
            }

            originalColor = baseColor;
            spriteRenderer.color = baseColor;
            return;
        }

        tileType = "empty";
        owner = "none";
        originalColor = baseColor;
        spriteRenderer.color = baseColor;
    }

    public void RemoveWorldEffect()
    {
        hasWorldEffect = false;
        worldEffectOwner = "none";
        worldEffectAllowsUnitPassThrough = false;
        worldEffectAllowsUnitOccupancy = false;
        ClearFieldData();
        ClearWorldEffectVisual();

        if (tileType == "worldEffect")
        {
            tileType = "empty";
            owner = "none";
            originalColor = baseColor;
            spriteRenderer.color = baseColor;
            return;
        }

        if (tileType == "unit")
        {
            originalColor = owner == "player" ? PlayerUnitTileColor : EnemyUnitTileColor;
            spriteRenderer.color = originalColor;
            return;
        }

        originalColor = baseColor;
        spriteRenderer.color = baseColor;
    }

    public void SetFieldData(string clusterId, int hpPerTile, int bonusMoneyPerTurn = 1)
    {
        if (!hasWorldEffect)
        {
            return;
        }

        isFieldTile = true;
        fieldClusterId = string.IsNullOrWhiteSpace(clusterId) ? string.Empty : clusterId;
        fieldHp = Mathf.Max(1, hpPerTile);
        fieldBonusMoneyPerTurn = Mathf.Max(0, bonusMoneyPerTurn);
        isMineTile = false;
        mineDamage = 0;
        isCampTile = false;
    }

    public void SetMineData(int damage)
    {
        if (!hasWorldEffect)
        {
            return;
        }

        isFieldTile = false;
        fieldClusterId = string.Empty;
        fieldHp = 0;
        fieldBonusMoneyPerTurn = 0;
        isMineTile = true;
        mineDamage = Mathf.Max(1, damage);
        isCampTile = false;
    }

    public void SetMineVisibility(bool isVisible)
    {
        if (!isMineTile)
        {
            return;
        }

        if (worldEffectRenderer != null)
        {
            worldEffectRenderer.enabled = isVisible;
        }
    }

    public void SetCampData()
    {
        if (!hasWorldEffect)
        {
            return;
        }

        isFieldTile = false;
        fieldClusterId = string.Empty;
        fieldHp = 0;
        fieldBonusMoneyPerTurn = 0;
        isMineTile = false;
        mineDamage = 0;
        isCampTile = true;
    }

    public void ClearWorldEffectSpecialData()
    {
        isFieldTile = false;
        fieldClusterId = string.Empty;
        fieldHp = 0;
        fieldBonusMoneyPerTurn = 0;
        isMineTile = false;
        mineDamage = 0;
        isCampTile = false;
    }

    public bool DamageField(int amount)
    {
        if (!hasWorldEffect || !isFieldTile)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        fieldHp -= safeAmount;

        if (fieldHp <= 0)
        {
            RemoveWorldEffect();
        }

        return true;
    }

    private void ClearFieldData()
    {
        ClearWorldEffectSpecialData();
    }

    private void SetWorldEffectVisual(Sprite effectSprite, float opacity = 1f)
    {
        worldEffectRenderer = EnsureOverlayRenderer(worldEffectRenderer, "WorldEffectVisual", 1);

        worldEffectRenderer.sprite = effectSprite;
        worldEffectRenderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
        FitOverlayVisualToTile(worldEffectRenderer, effectSprite, 1f, 0f);
        worldEffectRenderer.enabled = true;
    }

    private void SetFortVisual(Sprite fortSprite)
    {
        fortRenderer = EnsureOverlayRenderer(fortRenderer, "FortVisual", 2);
        fortRenderer.sprite = fortSprite;
        fortRenderer.color = Color.white;
        FitOverlayVisualToTile(fortRenderer, fortSprite, 0.92f, 0.08f);
        fortRenderer.enabled = true;
    }

    private SpriteRenderer EnsureOverlayRenderer(SpriteRenderer existingRenderer, string objectName, int sortingOrderOffset)
    {
        if (existingRenderer != null)
        {
            return existingRenderer;
        }

        GameObject visualObject = new GameObject(objectName);
        visualObject.transform.SetParent(transform, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;

        SpriteRenderer overlayRenderer = visualObject.AddComponent<SpriteRenderer>();
        overlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + sortingOrderOffset;
        return overlayRenderer;
    }

    private void FitOverlayVisualToTile(
        SpriteRenderer overlayRenderer,
        Sprite overlaySprite,
        float scaleMultiplier,
        float verticalOffsetInTileHeights)
    {
        if (overlayRenderer == null || spriteRenderer == null || overlaySprite == null)
        {
            return;
        }

        Vector2 tileSize = spriteRenderer.sprite != null
            ? spriteRenderer.sprite.bounds.size
            : Vector2.zero;
        Vector2 overlaySize = overlaySprite.bounds.size;

        if (tileSize.x <= 0f || tileSize.y <= 0f || overlaySize.x <= 0f || overlaySize.y <= 0f)
        {
            overlayRenderer.transform.localScale = Vector3.one;
            overlayRenderer.transform.localPosition = Vector3.zero;
            return;
        }

        float scale = Mathf.Min(tileSize.x / overlaySize.x, tileSize.y / overlaySize.y) * scaleMultiplier;
        overlayRenderer.transform.localScale = new Vector3(scale, scale, 1f);
        overlayRenderer.transform.localPosition = new Vector3(
            0f,
            tileSize.y * verticalOffsetInTileHeights,
            0f);
    }

    private void ClearWorldEffectVisual()
    {
        if (worldEffectRenderer == null)
        {
            return;
        }

        worldEffectRenderer.sprite = null;
        worldEffectRenderer.color = Color.white;
        worldEffectRenderer.enabled = false;
    }

    private void ClearFortVisual()
    {
        if (fortRenderer == null)
        {
            return;
        }

        fortRenderer.sprite = null;
        fortRenderer.color = Color.white;
        fortRenderer.enabled = false;
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
