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

    protected float lastAttackTime = Mathf.NegativeInfinity;

    protected Animator animator;
    protected Character character;

    private void Start()
    {
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
    }

    public virtual float GetAttackEffectiveness()
    {
        return 0.5f;
    }

    public virtual void Attack()
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

    protected void ApplyDamageAndThreat(Character target)
    {
        target.Damage(damage);
        float threatGenerated = damage * threatMultiplier;
        target.AddThreat(GetComponent<Character>(), threatGenerated);
    }

    protected Character GetTarget()
    {
        if (character.IsEnemy)
        {
            return GetEnemyTarget();
        }

        Character oppositeCharacter = CharacterManager.Instance.GetOppositeCharacter(character);

        if (oppositeCharacter == null)
            return null;

        var targetDistribution =
            character.Role == CharacterRole.Tank
                ? GetTankTargetDistribution()
                : GetDamageTargetDistribution();

        var oppositeCharacterDistribution = new Dictionary<Character, float>()
        {
            { oppositeCharacter, 1f },
        };

        var combinedDistribution = DistributionUtils.CombineDistributions(
            targetDistribution,
            oppositeCharacterDistribution,
            Mathf.Max(character.Intelligence, 1f - 0.1f),
            Mathf.Max(1f - character.Intelligence, 0.1f)
        );

        return DistributionUtils.SampleDistribution(combinedDistribution, 0.1f);
    }

    private Character GetEnemyTarget()
    {
        Character highestThreatTarget = character.GetHighestThreatTarget();
        if (highestThreatTarget != null)
        {
            return highestThreatTarget;
        }

        Character oppositeCharacter = CharacterManager.Instance.GetOppositeCharacter(character);
        if (oppositeCharacter != null)
        {
            return oppositeCharacter;
        }

        return null;
    }

    private Dictionary<Character, float> GetTankTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        // Calculate threat values for each enemy
        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            float threatValue = 1f;

            // Get current threat this tank has on this enemy
            float currentThreat = enemy.ThreatTable.ContainsKey(character)
                ? enemy.ThreatTable[character]
                : 0f;

            // Get the highest threat on this enemy from any character
            float highestThreat = 0f;
            foreach (var threatEntry in enemy.ThreatTable.Values)
            {
                if (threatEntry > highestThreat)
                    highestThreat = threatEntry;
            }

            // If we're not the highest threat, increase our priority
            if (currentThreat < highestThreat)
            {
                threatValue += (highestThreat - currentThreat) * 2f; // Strongly prioritize gaining threat
            }

            // Add power level bonus (higher power = higher priority)
            threatValue += enemy.EnemyPowerLevel * 0.5f;

            // If we have no threat on this enemy, give it extra priority
            if (currentThreat <= 0f)
            {
                threatValue += 5f; // High priority for enemies we have no threat on
            }

            distribution[enemy] = threatValue;
        }

        return DistributionUtils.NormalizeDistribution(distribution);
    }

    private Dictionary<Character, float> GetDamageTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        // Calculate threat values for each enemy
        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            float threatValue = 1f;

            // Get current threat this damage dealer has on this enemy
            float currentThreat = enemy.ThreatTable.ContainsKey(character)
                ? enemy.ThreatTable[character]
                : 0f;

            // Get the highest threat on this enemy from any character
            float highestThreat = 0f;
            foreach (var threatEntry in enemy.ThreatTable.Values)
            {
                if (threatEntry > highestThreat)
                    highestThreat = threatEntry;
            }

            // Add power level bonus (lower power = Higher priority for damage dealers)
            threatValue += 5f / Mathf.Max(enemy.EnemyPowerLevel, 1f);

            // If we're the highest threat, heavily discourage attacking this target
            if (currentThreat >= highestThreat && highestThreat > 0f)
            {
                threatValue *= 0.1f; // 90% reduction in priority
            }

            // Ensure minimum threat value
            threatValue = Mathf.Max(threatValue, 0.1f);

            distribution[enemy] = threatValue;
        }

        return DistributionUtils.NormalizeDistribution(distribution);
    }
}
