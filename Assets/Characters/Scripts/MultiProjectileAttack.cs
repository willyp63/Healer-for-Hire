using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiProjectileAttack : CharacterAttack
{
    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private float projectileSpeed = 10f;
    public float ProjectileSpeed => projectileSpeed;

    [SerializeField]
    private Transform projectileSpawnPoint;

    [SerializeField]
    private float delayBetweenProjectiles = 0.1f;

    public override void Attack(Character target)
    {
        base.Attack(target);

        // Ignore the target parameter and fire at all enemies
        StartCoroutine(PerformMultiAttack());
    }

    private IEnumerator PerformMultiAttack()
    {
        // Wait for animation to reach the right point
        yield return new WaitForSeconds(animationDelay);

        // Get all active enemy characters
        List<Character> enemies = CharacterManager.Instance.GetActiveEnemyCharacters();

        if (enemies.Count == 0)
            yield break;

        // Fire a projectile at each enemy
        foreach (Character enemy in enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0)
            {
                // Spawn projectile
                Vector3 spawnPosition =
                    projectileSpawnPoint != null
                        ? projectileSpawnPoint.position
                        : transform.position;

                GameObject projectile = Instantiate(
                    projectilePrefab,
                    spawnPosition,
                    Quaternion.identity
                );
                Projectile projectileComponent = projectile.GetComponent<Projectile>();

                if (projectileComponent != null)
                {
                    projectileComponent.Initialize(enemy, character, this, projectileSpeed);
                }
                else
                {
                    Debug.LogError("Projectile component not found on projectile prefab");
                }

                // Small delay between projectiles
                yield return new WaitForSeconds(delayBetweenProjectiles);
            }
        }
    }
}
