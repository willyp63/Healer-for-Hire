using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [SerializeField]
    private bool isDebugMode = false;

    private Character character;

    private const float MIN_TEMPERATURE = 0.05f;

    private void Start()
    {
        character = GetComponent<Character>();
    }

    public (CharacterAttack attack, Character target) MakeDecision()
    {
        if (isDebugMode)
        {
            Debug.Log("\n\n");
            Debug.Log("\n\n");
            Debug.Log("\n\n");
            Debug.Log("Making Decision...");
        }

        var attackDistribution = GetAttackDistribution();

        if (isDebugMode)
        {
            Debug.Log("Attack Distribution:");
            DistributionUtils.PrintDistribution(attackDistribution);
        }

        var highestPriority = 0f;
        foreach (var attack in attackDistribution)
            if (attack.Value > highestPriority)
                highestPriority = attack.Value;

        var shouldDoNothingDistribution = new Dictionary<bool, float>()
        {
            { true, 1f - highestPriority },
            { false, highestPriority },
        };

        if (isDebugMode)
        {
            Debug.Log("Do Nothing Distribution:");
            DistributionUtils.PrintDistribution(shouldDoNothingDistribution);
        }

        var shouldDoNothing = DistributionUtils.SampleDistribution(
            shouldDoNothingDistribution,
            character.AITemperature * 0.1f // lower temp cause we need to keep doing nothing (TODO: better system)
        );

        if (shouldDoNothing)
        {
            if (isDebugMode)
            {
                Debug.Log("Doing Nothing");
                GameManager.Instance.PauseGame();
            }

            return (null, null);
        }

        var bestAttack = DistributionUtils.SampleDistribution(
            attackDistribution,
            character.AITemperature
        );

        if (bestAttack == null)
        {
            if (isDebugMode)
            {
                Debug.Log("No Attack Found");
                GameManager.Instance.PauseGame();
            }

            return (null, null);
        }

        var target = GetTarget(bestAttack);

        if (target == null)
        {
            if (isDebugMode)
            {
                Debug.Log("No Target Found");
                GameManager.Instance.PauseGame();
            }

            return (null, null);
        }

        if (isDebugMode)
        {
            Debug.Log("Decision Made!");
            Debug.Log("Target: " + target);
            Debug.Log("Attack: " + bestAttack);
            GameManager.Instance.PauseGame();
        }

        return (bestAttack, target);
    }

    private Dictionary<CharacterAttack, float> GetAttackDistribution()
    {
        Dictionary<CharacterAttack, float> attackDistribution = new();

        foreach (var attack in character.Attacks)
        {
            if (attack.IsOnCooldown())
                continue;

            attackDistribution[attack] = GetAttackPriority(attack);
        }

        return attackDistribution;
    }

    private float GetAttackPriority(CharacterAttack attack)
    {
        float priority =
            attack.LowAIPriority
            + (attack.HighAIPriority - attack.LowAIPriority) * character.AIStrength;

        foreach (var condition in attack.PriorityConditions)
        {
            if (IsConditionMet(condition))
            {
                priority +=
                    condition.lowAIPriorityAddition
                    + (condition.highAIPriorityAddition - condition.lowAIPriorityAddition)
                        * character.AIStrength;
            }
        }

        return Mathf.Clamp(priority, 0f, 1f);
    }

    private bool IsConditionMet(PriorityCondition condition)
    {
        float value = GetConditionValue(condition);
        switch (condition.operatorType)
        {
            case OperatorType.GreaterThan:
                return value > condition.value;
            case OperatorType.LessThan:
                return value < condition.value;
            case OperatorType.EqualTo:
                return value == condition.value;
            case OperatorType.NotEqualTo:
                return value != condition.value;
            default:
                return false;
        }
    }

    private float GetConditionValue(PriorityCondition condition)
    {
        switch (condition.conditionType)
        {
            case ConditionType.NumEnemies:
                var enemies = CharacterManager.Instance.GetActiveEnemyCharacters();
                int enemiyCount = 0;
                foreach (var enemy in enemies)
                    if (enemy != null)
                        enemiyCount++;
                return enemiyCount;
            case ConditionType.NumEnemiesWithThreat:
                var enemiesWithHighestThreat = CharacterManager.Instance.GetActiveEnemyCharacters();
                int highThreatCount = 0;
                foreach (var enemy in enemiesWithHighestThreat)
                    if (enemy != null && enemy.GetHighestThreatTarget() == character)
                        highThreatCount++;
                return highThreatCount;
            case ConditionType.NumEnemiesWithoutThreat:
                var enemiesWithoutThreat = CharacterManager.Instance.GetActiveEnemyCharacters();
                int lowThreatCount = 0;
                foreach (var enemy in enemiesWithoutThreat)
                    if (enemy != null && enemy.GetHighestThreatTarget() != character)
                        lowThreatCount++;
                return lowThreatCount;
            case ConditionType.Health:
                return (float)character.CurrentHealth / character.MaxHealth;
            case ConditionType.Resource:
                return (float)character.CurrentResource / character.MaxResource;
            default:
                return 0f;
        }
    }

    private Character GetTarget(CharacterAttack attack)
    {
        if (character.IsEnemy)
            return GetEnemyTarget();

        // Early return if any targeting weight is All or Self
        foreach (var weight in attack.TargetingWeights)
        {
            if (
                weight.targetingType == AttackTargetingType.All
                || weight.targetingType == AttackTargetingType.Self
            )
            {
                return character;
            }
        }

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

        if (isDebugMode)
        {
            Debug.Log("AI Distribution:");
            DistributionUtils.PrintDistribution(aiDistribution);
        }

        return DistributionUtils.SampleDistribution(aiDistribution, character.AITemperature);
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

            // Get current threat this tank has on this enemy
            float currentThreat = enemy.ThreatTable.ContainsKey(character)
                ? enemy.ThreatTable[character]
                : 0f;

            // Get the highest threat on this enemy from any character
            float highestThreat = 0f;
            float secondHighestThreat = 0f;
            foreach (var threatEntry in enemy.ThreatTable.Values)
            {
                if (threatEntry > highestThreat)
                {
                    secondHighestThreat = highestThreat;
                    highestThreat = threatEntry;
                }
                else if (threatEntry > secondHighestThreat)
                {
                    secondHighestThreat = threatEntry;
                }
            }

            bool isHighestThreat = currentThreat == highestThreat;

            if (highestThreat == 0f)
            {
                // prioritize enemies with no threat
                distribution[enemy] = 1.5f;
            }
            else if (isHighestThreat)
            {
                // range from 0 (we have a huge lead) to 1 (we barely have a lead)
                distribution[enemy] = secondHighestThreat / highestThreat;
            }
            else
            {
                // range from 1.5 (we are barely behind) to 5 (we are way behind)
                distribution[enemy] = 1.5f + 3.5f * (1f - currentThreat / highestThreat);
            }
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

            // Get current threat this damage dealer has on this enemy
            float currentThreat = enemy.ThreatTable.ContainsKey(character)
                ? enemy.ThreatTable[character]
                : 0f;

            // Get the highest threat on this enemy from any character
            float highestThreat = 0f;
            foreach (var threatEntry in enemy.ThreatTable.Values)
            {
                if (threatEntry > highestThreat)
                {
                    highestThreat = threatEntry;
                }
            }

            float threatValue = 1f;

            // If no one has threat on this enemy, then AVOID this enemy (high risk of pulling aggro)
            // If we are the highest threat on this enemy, then AVOID this enemy (high risk of pulling aggro)
            if (highestThreat == 0f || currentThreat >= highestThreat)
            {
                threatValue = 0f;
            }
            // If we are not the highest threat on this enemy, then prioritize by how far we are from the highest threat
            else if (currentThreat < highestThreat)
            {
                // The more gap between our threat and the highest threat, the safer it is to attack
                float threatGap = highestThreat - currentThreat;
                threatValue = threatGap / highestThreat; // Normalize by highest threat
            }

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
