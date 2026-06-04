using System.Collections;
using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    [Min(0.01f)]
    public float defaultSpeed = 8f;

    public bool rotateTowardTravel = true;
    public GameObject defaultImpactPrefab;
    public float destroyDelay = 0.05f;

    public static ProjectileVisual Spawn(
        GameObject prefab,
        Vector3 startPosition,
        Vector3 targetPosition,
        float speed,
        GameObject impactPrefab = null)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject projectileObject = Instantiate(prefab, startPosition, Quaternion.identity);
        ProjectileVisual projectile = projectileObject.GetComponent<ProjectileVisual>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<ProjectileVisual>();
        }

        projectile.Launch(startPosition, targetPosition, speed, impactPrefab);
        return projectile;
    }

    public static ProjectileVisual Spawn(ProjectileVisualSettings settings, Vector3 startPosition, Vector3 targetPosition)
    {
        if (settings == null || settings.projectilePrefab == null)
        {
            return null;
        }

        Vector3 spawnPosition = startPosition + settings.spawnOffset;
        return Spawn(
            settings.projectilePrefab,
            spawnPosition,
            targetPosition,
            settings.projectileSpeed,
            settings.impactPrefab);
    }

    public void Launch(
        Vector3 startPosition,
        Vector3 targetPosition,
        float speed = -1f,
        GameObject impactPrefabOverride = null)
    {
        transform.position = startPosition;
        StopAllCoroutines();
        StartCoroutine(TravelRoutine(targetPosition, speed, impactPrefabOverride));
    }

    private IEnumerator TravelRoutine(
        Vector3 targetPosition,
        float speedOverride,
        GameObject impactPrefabOverride)
    {
        float moveSpeed = speedOverride > 0f ? speedOverride : defaultSpeed;
        moveSpeed = Mathf.Max(0.01f, moveSpeed);

        while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
            Vector3 travelDirection = nextPosition - currentPosition;

            if (rotateTowardTravel && travelDirection.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            transform.position = nextPosition;
            yield return null;
        }

        GameObject impactPrefab = impactPrefabOverride != null ? impactPrefabOverride : defaultImpactPrefab;
        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, targetPosition, Quaternion.identity);
        }

        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(destroyDelay);
        }

        Destroy(gameObject);
    }
}
