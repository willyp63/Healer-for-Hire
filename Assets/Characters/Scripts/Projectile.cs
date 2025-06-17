using System.Collections;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Character target;
    private Character attacker;
    private ProjectileAttack attack;

    private bool isHit = false;

    private Animator animator;

    public void Initialize(Character target, Character attacker, ProjectileAttack attack)
    {
        this.target = target;
        this.attacker = attacker;
        this.attack = attack;

        animator = GetComponentInChildren<Animator>();
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
        Vector3 targetPosition = target.transform.position + new Vector3(0f, 0.5f, 0f);
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * attack.ProjectileSpeed * Time.deltaTime;

        // Check if we've hit the target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isHit = true;

            CharacterAttack.ApplyDamageAndThreat(target, attacker, attack);

            animator.SetTrigger("Hit");

            StartCoroutine(DestroyAfterAnimation());
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}
