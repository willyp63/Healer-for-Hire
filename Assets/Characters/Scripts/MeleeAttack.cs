using System.Collections;
using UnityEngine;

public class MeleeAttack : CharacterAttack
{
    [SerializeField]
    private float forwardDuration = 0.167f;

    [SerializeField]
    private float forwardPauseDuration = 0f;

    [SerializeField]
    private float backwardPauseDuration = 0f;

    [SerializeField]
    private float backwardDuration = 0.167f;

    [SerializeField]
    private float attackRange = 0.3f;

    public override void Attack(Character target)
    {
        base.Attack(target);

        StartCoroutine(PerformAttack(target));
    }

    private IEnumerator PerformAttack(Character target)
    {
        if (target == null)
            yield break;

        // Store original position
        Vector3 originalPosition = CharacterManager.Instance.GetCharacterSlotPosition(character);
        Vector3 targetPosition = CharacterManager.Instance.GetCharacterSlotPosition(target);

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

        yield return new WaitForSeconds(forwardPauseDuration);

        // Apply damage if we're close enough to the target
        if (
            target != null
            && Vector3.Distance(transform.position, target.transform.position) <= attackRange
        )
        {
            CharacterAttack.ApplyDamageAndThreat(target, character, this);
        }
        else
        {
            FloatingTextManager.Instance.SpawnText("MISS", transform.position, Color.gray);
        }

        yield return new WaitForSeconds(backwardPauseDuration);

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
