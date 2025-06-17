using System.Collections;
using UnityEngine;

public class MeleeAttack : CharacterAttack
{
    [SerializeField]
    private float forwardDuration = 0.3f;

    [SerializeField]
    private float backwardDuration = 0.3f;

    [SerializeField]
    private float attackRange = 0.3f;

    public override void Attack()
    {
        base.Attack();

        StartCoroutine(PerformAttack(GetTarget()));
    }

    private IEnumerator PerformAttack(Character target)
    {
        if (target == null)
            yield break;

        // Store original position
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = target.transform.position;

        // Wait for animation to reach the right point
        yield return new WaitForSeconds(animationDelay);

        // Dash to target
        float elapsed = 0f;

        while (elapsed < forwardDuration && target != null)
        {
            transform.position = Vector3.Lerp(
                originalPosition,
                targetPosition,
                elapsed / forwardDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Apply damage if we're close enough to the target
        if (
            target != null
            && Vector3.Distance(transform.position, target.transform.position) <= attackRange
        )
        {
            ApplyDamageAndThreat(target);
        }

        // Dash back to original position
        elapsed = 0f;
        Vector3 currentPosition = transform.position;

        while (elapsed < backwardDuration)
        {
            transform.position = Vector3.Lerp(
                currentPosition,
                originalPosition,
                elapsed / backwardDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we're exactly at the original position
        transform.position = originalPosition;
    }
}
