using System.Collections;
using UnityEngine;

public class ProjectileAttack : CharacterAttack
{
    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private float projectileSpeed = 10f;
    public float ProjectileSpeed => projectileSpeed;

    [SerializeField]
    private Transform projectileSpawnPoint;

    public override void Attack(Character target)
    {
        base.Attack(target);

        StartCoroutine(PerformAttack(target));
    }

    private IEnumerator PerformAttack(Character target)
    {
        // Wait for animation to reach the right point
        yield return new WaitForSeconds(animationDelay);

        if (target == null)
            yield break;

        // Spawn projectile
        Vector3 spawnPosition =
            projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectileComponent = projectile.GetComponent<Projectile>();

        if (projectileComponent != null)
        {
            projectileComponent.Initialize(target, character, this, projectileSpeed);
        }
        else
        {
            Debug.LogError("Projectile component not found on projectile prefab");
        }
    }
}
