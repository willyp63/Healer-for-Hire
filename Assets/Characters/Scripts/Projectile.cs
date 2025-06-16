using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Character target;
    private int damage;
    private float threatMultiplier;
    private Character source;
    private float speed = 10f;

    public void Initialize(Character target, int damage, float threatMultiplier, Character source)
    {
        this.target = target;
        this.damage = damage;
        this.threatMultiplier = threatMultiplier;
        this.source = source;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards target
        Vector3 targetPosition = target.transform.position + new Vector3(0f, 0.5f, 0f);
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Check if we've hit the target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            target.Damage(damage);
            float threatGenerated = damage * threatMultiplier;
            target.AddThreat(source, threatGenerated);
            Destroy(gameObject);
        }
    }
}
