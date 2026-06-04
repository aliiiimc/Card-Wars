using UnityEngine;

[System.Serializable]
public class ProjectileVisualSettings
{
    public GameObject projectilePrefab;
    public GameObject impactPrefab;

    [Min(0.01f)]
    public float projectileSpeed = 8f;

    public Vector3 spawnOffset = new Vector3(0f, 0.2f, 0f);
}
