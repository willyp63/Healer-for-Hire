using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterAttack : MonoBehaviour
{
    [SerializeField]
    protected int damage = 0;

    [SerializeField]
    protected float cooldown = 1f;
    public float Cooldown => cooldown;

    [SerializeField]
    protected float threatMultiplier = 1f;

    [SerializeField]
    protected float animationDelay = 0.2f;

    [SerializeField]
    protected string attackAnimationTrigger = "Attack";

    protected Animator animator;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();
    }

    public virtual void Attack(Character target)
    {
        if (animator != null)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }

        StartCoroutine(PerformAttack(target));
    }

    protected abstract IEnumerator PerformAttack(Character target);

    protected void ApplyDamageAndThreat(Character target)
    {
        target.Damage(damage);
        float threatGenerated = damage * threatMultiplier;
        target.AddThreat(GetComponent<Character>(), threatGenerated);
    }
}
