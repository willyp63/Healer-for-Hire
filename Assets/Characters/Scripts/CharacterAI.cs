using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    private Character character;

    private const float MIN_TEMPERATURE = 0.05f;

    private void Start()
    {
        character = GetComponent<Character>();
    }

    public (CharacterAttack attack, Character target) MakeDecision()
    {
        var bestAttack = DistributionUtils.SampleDistribution(
            GetAttackDistribution(),
            Mathf.Max(1f - character.Intelligence, 0.1f)
        );

        if (bestAttack == null)
            return (null, null);

        var target = GetTarget(bestAttack);
        if (target == null)
            return (null, null);

        return (bestAttack, target);
    }

    // TODO: factor in resource management
    private Dictionary<CharacterAttack, float> GetAttackDistribution()
    {
        Dictionary<CharacterAttack, float> attackDistribution = new();

        foreach (var attack in character.Attacks)
        {
            if (attack.IsOnCooldown())
                continue;

            attackDistribution[attack] = attack.GetAttackEffectiveness();
        }

        return attackDistribution;
    }

    private Character GetTarget(CharacterAttack attack)
    {
        if (character.IsEnemy)
            return GetEnemyTarget();

        var closestDistribution = GetClosestTargetDistribution();
        if (closestDistribution.Count == 0)
            return null;

        // Combine all of the AI targeting distributions
        var aiDistribution = DistributionUtils.CombineDistributions(
            Array.ConvertAll(
                attack.TargetingWeights,
                w =>
                    DistributionUtils.NormalizeDistribution(
                        GetTargetDistribution(attack, w.targetingType)
                    )
            ),
            Array.ConvertAll(attack.TargetingWeights, w => w.weight)
        );

        // Combine AI targeting distribution with closest target distribution.
        // The higher the intelligence, the more weight is given to the AI targeting distribution
        var combinedDistribution = DistributionUtils.CombineDistributions(
            new Dictionary<Character, float>[]
            {
                DistributionUtils.NormalizeDistribution(aiDistribution),
                closestDistribution,
            },
            new float[]
            {
                Mathf.Max(character.Intelligence, 1f - MIN_TEMPERATURE),
                Mathf.Max(1f - character.Intelligence, MIN_TEMPERATURE),
            }
        );

        return DistributionUtils.SampleDistribution(combinedDistribution, MIN_TEMPERATURE);
    }

    private Dictionary<Character, float> GetTargetDistribution(
        CharacterAttack attack,
        AttackTargetingType targetingType
    )
    {
        switch (targetingType)
        {
            case AttackTargetingType.Closest:
                return GetClosestTargetDistribution();
            case AttackTargetingType.TankThreat:
                return GetTankThreatDistribution();
            case AttackTargetingType.DamageThreat:
                return GetDamageThreatDistribution();
            case AttackTargetingType.HighestHealth:
                return GetHighestHealthTargetDistribution();
            case AttackTargetingType.LowestHealth:
                return GetLowestHealthTargetDistribution();
            case AttackTargetingType.HighestPowerLevel:
                return GetHighestPowerLevelTargetDistribution();
            case AttackTargetingType.LowestPowerLevel:
                return GetLowestPowerLevelTargetDistribution();
            case AttackTargetingType.All:
                return GetAllTargetDistribution();
            case AttackTargetingType.Self:
                return GetSelfTargetDistribution();
            default:
                return new Dictionary<Character, float>();
        }
    }

    private Character GetEnemyTarget()
    {
        Character highestThreatTarget = character.GetHighestThreatTarget();
        if (highestThreatTarget != null)
        {
            return highestThreatTarget;
        }

        Character closestTarget = CharacterManager.Instance.GetClosestTarget(character);
        if (closestTarget != null)
        {
            return closestTarget;
        }

        return null;
    }

    private Dictionary<Character, float> GetClosestTargetDistribution()
    {
        Character closestTarget = CharacterManager.Instance.GetClosestTarget(character);
        if (closestTarget == null)
            return new Dictionary<Character, float>() { };

        return new Dictionary<Character, float>() { { closestTarget, 1f } };
    }

    private Dictionary<Character, float> GetTankThreatDistribution()
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

        return distribution;
    }

    private Dictionary<Character, float> GetDamageThreatDistribution()
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

        return distribution;
    }

    private Dictionary<Character, float> GetHighestHealthTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            // Higher health = higher priority
            distribution[enemy] = enemy.CurrentHealth;
        }

        return distribution;
    }

    private Dictionary<Character, float> GetLowestHealthTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            // Lower health = higher priority
            // Add 1 to avoid division by zero
            distribution[enemy] = 1f / (enemy.CurrentHealth + 1f);
        }

        return distribution;
    }

    private Dictionary<Character, float> GetHighestPowerLevelTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            // Higher power level = higher priority
            distribution[enemy] = enemy.EnemyPowerLevel;
        }

        return distribution;
    }

    private Dictionary<Character, float> GetLowestPowerLevelTargetDistribution()
    {
        var enemyCharacters = CharacterManager.Instance.GetActiveEnemyCharacters();
        var distribution = new Dictionary<Character, float>();

        if (enemyCharacters.Count == 0)
            return distribution;

        foreach (var enemy in enemyCharacters)
        {
            if (enemy == null)
                continue;

            // Lower power level = higher priority
            // Add 1 to avoid division by zero
            distribution[enemy] = 1f / (enemy.EnemyPowerLevel + 1f);
        }

        return distribution;
    }

    private Dictionary<Character, float> GetAllTargetDistribution()
    {
        // For "All" targeting type, we don't care about the target distribution
        // as the attack will hit all targets anyway
        return new Dictionary<Character, float>();
    }

    private Dictionary<Character, float> GetSelfTargetDistribution()
    {
        // For "Self" targeting type, we only target ourselves
        return new Dictionary<Character, float>() { { character, 1f } };
    }
}
