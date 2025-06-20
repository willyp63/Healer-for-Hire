using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private readonly float HIT_RANGE = 0.25f;
    private readonly Vector3 HIT_OFFSET = new Vector3(0f, 0.5f, 0f);

    private Character target;
    private Character attacker;
    private CharacterAttack attack;

    private float projectileSpeed;
    private bool isHit = false;

    private Vector3 targetPosition;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public void Initialize(
        Character target,
        Character attacker,
        CharacterAttack attack,
        float projectileSpeed
    )
    {
        this.target = target;
        this.attacker = attacker;
        this.attack = attack;
        this.projectileSpeed = projectileSpeed;

        targetPosition = CharacterManager.Instance.GetCharacterSlotPosition(target) + HIT_OFFSET;

        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isHit)
            return;

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards target
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * projectileSpeed * Time.deltaTime;

        // Update sprite orientation to face movement direction
        UpdateSpriteOrientation(direction);

        // Check if we've hit the target
        if (
            Vector3.Distance(transform.position, target.transform.position + HIT_OFFSET) < HIT_RANGE
        )
        {
            isHit = true;

            CharacterAttack.ApplyDamageAndThreat(target, attacker, attack);

            animator.SetTrigger("Hit");

            StartCoroutine(DestroyAfterAnimation());
        }

        // check if we've reached the target slot
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isHit = true;

            FloatingTextManager.Instance.SpawnText("MISS", transform.position, Color.gray);

            Destroy(gameObject);
        }
    }

    private void UpdateSpriteOrientation(Vector3 direction)
    {
        if (spriteRenderer == null)
            return;

        // Calculate the angle to rotate the sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Reset any previous rotation and apply the new rotation
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Handle horizontal flipping for leftward movement
        // Since the default sprite faces right, we need to flip when moving left
        if (direction.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}
