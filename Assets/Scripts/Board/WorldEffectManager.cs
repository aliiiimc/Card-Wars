using System.Collections.Generic;
using UnityEngine;

public class WorldEffectManager : MonoBehaviour
{
    private readonly Dictionary<HexTile, WorldEffect> worldEffectsByTile = new Dictionary<HexTile, WorldEffect>();

    public bool TryPlaceFromCard(HexTile tile, string owner, CardRuntimeState card, out WorldEffect worldEffect)
    {
        worldEffect = null;

        if (tile == null || card == null || !(card.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        if (!tile.CanPlaceWorldEffect())
        {
            return false;
        }

        worldEffect = CreateWorldEffectFromCard(tile, owner, card, worldEffectCard);
        return worldEffect != null;
    }

    public bool TryColonize(HexTile tile, string newOwner)
    {
        if (tile == null || string.IsNullOrWhiteSpace(newOwner))
        {
            return false;
        }

        if (!tile.HasWorldEffect() || tile.worldEffectOwner == "none" || tile.worldEffectOwner == newOwner)
        {
            return false;
        }

        tile.SetWorldEffectOwner(newOwner);

        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect existing) && existing != null)
        {
            existing.owner = newOwner;
            return true;
        }

        return true;
    }

    public bool TryReplace(HexTile tile, string owner, CardRuntimeState card, out WorldEffect worldEffect)
    {
        worldEffect = null;

        if (tile == null || card == null || !(card.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        if (!tile.HasWorldEffect() || tile.HasUnitOccupant())
        {
            return false;
        }

        worldEffect = CreateWorldEffectFromCard(tile, owner, card, worldEffectCard);
        return worldEffect != null;
    }

    public bool Remove(HexTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect worldEffect))
        {
            worldEffectsByTile.Remove(tile);
            if (worldEffect != null)
            {
                worldEffect.RemoveFromBoard();
            }
            else
            {
                tile.RemoveWorldEffect();
            }
            return true;
        }

        if (tile.HasWorldEffect())
        {
            tile.RemoveWorldEffect();
            return true;
        }

        return false;
    }

    public bool TrySetFieldData(HexTile tile, string clusterId, int hpPerTile, int bonusMoneyPerTurn = 1)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetFieldData(clusterId, hpPerTile, bonusMoneyPerTurn);
        return true;
    }

    public bool TrySetMineData(HexTile tile, int damage)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetMineData(damage);
        return true;
    }

    public bool TryClearSpecialData(HexTile tile)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.ClearWorldEffectSpecialData();
        return true;
    }

    public bool TrySetCampData(HexTile tile)
    {
        if (!IsOwnedWorldEffectTile(tile))
        {
            return false;
        }

        tile.SetCampData();
        return true;
    }

    public bool TryDamageField(HexTile tile, int amount)
    {
        if (tile == null || !tile.HasWorldEffect() || !tile.isFieldTile)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        tile.fieldHp -= safeAmount;

        if (tile.fieldHp <= 0)
        {
            return Remove(tile);
        }

        return true;
    }

    public bool TryDamageWorldEffect(HexTile tile, int amount, out int dealtDamage)
    {
        // (abdo :) Shared damage path for fields and structures, used by normal combat and special attacks.
        dealtDamage = 0;

        if (tile == null || !tile.HasWorldEffect() || amount <= 0)
        {
            return false;
        }

        int safeAmount = Mathf.Max(1, amount);
        if (tile.isFieldTile)
        {
            int fieldHpBefore = Mathf.Max(0, tile.fieldHp);
            bool damaged = TryDamageField(tile, safeAmount);
            if (!damaged)
            {
                return false;
            }

            int fieldHpAfter = tile.HasWorldEffect() && tile.isFieldTile
                ? Mathf.Max(0, tile.fieldHp)
                : 0;
            dealtDamage = Mathf.Max(0, fieldHpBefore - fieldHpAfter);
            return true;
        }

        WorldEffect worldEffect = FindWorldEffectOnTile(tile);
        if (worldEffect == null)
        {
            dealtDamage = safeAmount;
            return Remove(tile);
        }

        int hpBefore = worldEffect.sourceCard != null && worldEffect.sourceCard.CurrentHp.HasValue
            ? Mathf.Max(0, worldEffect.sourceCard.CurrentHp.Value)
            : Mathf.Max(0, worldEffect.health);

        if (worldEffect.sourceCard != null)
        {
            worldEffect.sourceCard.ApplyDamage(safeAmount);
            worldEffect.health = worldEffect.sourceCard.CurrentHp.HasValue
                ? worldEffect.sourceCard.CurrentHp.Value
                : Mathf.Max(0, worldEffect.health - safeAmount);
        }
        else
        {
            worldEffect.health = Mathf.Max(0, worldEffect.health - safeAmount);
        }

        dealtDamage = Mathf.Max(0, hpBefore - Mathf.Max(0, worldEffect.health));
        if (worldEffect.health <= 0)
        {
            return Remove(tile);
        }

        return true;
    }

    // Backward-compatible wrapper while call sites migrate.
    public WorldEffect SpawnWorldEffectFromCard(HexTile tile, string owner, CardRuntimeState card)
    {
        return TryPlaceFromCard(tile, owner, card, out WorldEffect worldEffect) ? worldEffect : null;
    }

    // Backward-compatible wrapper while call sites migrate.
    public bool RemoveWorldEffect(HexTile tile)
    {
        return Remove(tile);
    }

    public WorldEffect FindWorldEffectOnTile(HexTile tile)
    {
        if (tile == null)
        {
            return null;
        }

        worldEffectsByTile.TryGetValue(tile, out WorldEffect worldEffect);
        return worldEffect;
    }

    public bool TryGetAttackProfile(HexTile sourceTile, out AttackType attackType, out AttackTarget attackTarget)
    {
        attackType = AttackType.Projectile;
        attackTarget = AttackTarget.Ground;

        if (sourceTile == null || !sourceTile.HasWorldEffect())
        {
            return false;
        }

        WorldEffect worldEffect = FindWorldEffectOnTile(sourceTile);
        if (worldEffect == null || worldEffect.sourceCard == null || !(worldEffect.sourceCard.SourceCard is WorldEffectCardData worldEffectCard))
        {
            return false;
        }

        attackType = worldEffectCard.attackType;
        attackTarget = worldEffectCard.attackTarget;
        return true;
    }

    public bool CanTargetWithProfile(HexTile sourceTile, bool targetIsAir)
    {
        if (!TryGetAttackProfile(sourceTile, out _, out AttackTarget attackTarget))
        {
            return false;
        }

        if (attackTarget == AttackTarget.Both)
        {
            return true;
        }

        if (targetIsAir)
        {
            return attackTarget == AttackTarget.Air;
        }

        return attackTarget == AttackTarget.Ground;
    }

    private WorldEffect CreateWorldEffectFromCard(HexTile tile, string owner, CardRuntimeState card, WorldEffectCardData worldEffectCard)
    {
        if (worldEffectsByTile.TryGetValue(tile, out WorldEffect existing) && existing != null)
        {
            existing.InitializeFromCard(card);
            existing.PlaceOnTile(
                tile,
                owner,
                worldEffectCard.manifestedSprite,
                worldEffectCard.allowsUnitPassThrough,
                worldEffectCard.allowsUnitOccupancy,
                worldEffectCard.worldEffectOpacity);
            return existing;
        }

        GameObject worldEffectObject = new GameObject($"WorldEffect_{worldEffectCard.DisplayName}");
        worldEffectObject.transform.SetParent(transform, true);

        WorldEffect worldEffect = worldEffectObject.AddComponent<WorldEffect>();
        worldEffect.InitializeFromCard(card);
        worldEffect.PlaceOnTile(
            tile,
            owner,
            worldEffectCard.manifestedSprite,
            worldEffectCard.allowsUnitPassThrough,
            worldEffectCard.allowsUnitOccupancy,
            worldEffectCard.worldEffectOpacity);

        worldEffectsByTile[tile] = worldEffect;
        return worldEffect;
    }

    private static bool IsOwnedWorldEffectTile(HexTile tile)
    {
        return tile != null
            && tile.HasWorldEffect()
            && !string.IsNullOrWhiteSpace(tile.worldEffectOwner)
            && tile.worldEffectOwner != "none";
    }
}
