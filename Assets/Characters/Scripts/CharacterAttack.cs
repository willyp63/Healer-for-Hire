using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackTargetingType
{
    Closest, // Closest enemy
    TankThreat, // Maintain threat as a tank
    DamageThreat, // Avoid threat as a damage dealer
    HighestHealth, // Highest health enemy
    LowestHealth, // Lowest health enemy
    HighestPowerLevel, // Highest power level enemy
    LowestPowerLevel, // Lowest power level enemy
    All, // Hits all enemies (not targeting needed)
    Self, // Always target self
}

public enum ConditionType
{
    NumEnemies,
    NumEnemiesWithThreat,
    NumEnemiesWithoutThreat,
    Health,
    Resource,
}

public enum OperatorType
{
    GreaterThan,
    LessThan,
    EqualTo,
    NotEqualTo,
}

[System.Serializable]
public struct TargetingWeight
{
    public AttackTargetingType targetingType;
    public float weight;
}

[System.Serializable]
public struct PriorityCondition
{
    public ConditionType conditionType;
    public OperatorType operatorType;
    public float value;

    public float priority;
}

public abstract class CharacterAttack : MonoBehaviour
{
    [SerializeField]
    protected float priority = 0;
    public float Priority => priority;

    [SerializeField]
    protected PriorityCondition[] priorityConditions;
    public PriorityCondition[] PriorityConditions => priorityConditions;

    [SerializeField]
    protected TargetingWeight[] targetingWeights;
    public TargetingWeight[] TargetingWeights => targetingWeights;

    [SerializeField]
    protected int damage = 0;
    public int Damage => damage;

    [SerializeField]
    protected float cooldown = 1f;
    public float Cooldown => cooldown;

    [SerializeField]
    protected float threatMultiplier = 1f;
    public float ThreatMultiplier => threatMultiplier;

    [SerializeField]
    protected StatusEffect statusEffectPrefab;
    public StatusEffect StatusEffectPrefab => statusEffectPrefab;

    [SerializeField]
    protected float animationDelay = 0.2f;

    [SerializeField]
    protected string attackAnimationTrigger = "Attack";

    protected float lastAttackTime = Mathf.NegativeInfinity;

    protected Animator animator;
    protected Character character;

    private void Start()
    {
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
    }

    public virtual void Attack(Character target)
    {
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(attackAnimationTrigger);
        }
    }

    public bool IsOnCooldown()
    {
        return Time.time - lastAttackTime < cooldown;
    }

    public static void ApplyDamageAndThreat(
        Character target,
        Character attacker,
        CharacterAttack attack
    )
    {
        if (attack.Damage > 0)
        {
            target.Damage(attack.Damage);

            float threatGenerated = attack.Damage * attack.ThreatMultiplier;
            target.AddThreat(attacker, threatGenerated);

            if (attacker.ResourceType == ResourceType.Rage)
            {
                attacker.AddResource(5 * (int)(attack.Damage * 100f / attacker.MaxHealth));
            }

            FloatingTextManager.Instance.SpawnDamage(attack.Damage, target.transform.position);
        }

        if (attack.StatusEffectPrefab != null)
        {
            StatusEffect statusEffect = Instantiate(attack.StatusEffectPrefab, target.transform);
            target.ApplyStatusEffect(statusEffect, attacker);
        }
    }
}
