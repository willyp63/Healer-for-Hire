using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [SerializeField]
    private bool isDebugMode = false;

    private Character character;

    private void Start()
    {
        character = GetComponent<Character>();
    }

    public CharacterAttack ChooseAttack()
    {
        if (isDebugMode)
        {
            Debug.Log("\n\n");
            Debug.Log("\n\n");
            Debug.Log("\n\n");
            Debug.Log("Choosing Attack...");
        }

        var attackDistribution = GetAttackDistribution();

        if (isDebugMode)
        {
            Debug.Log("Attack Distribution:");
            DistributionUtils.PrintDistribution(attackDistribution);
        }

        // Adjust attack distribution based on cooldowns and resource costs
        var adjustedAttackDistribution = AdjustAttackDistribution(attackDistribution);

        if (isDebugMode)
        {
            Debug.Log("Adjusted Attack Distribution:");
            DistributionUtils.PrintDistribution(adjustedAttackDistribution);
        }

        var bestAttack = DistributionUtils.SampleDistribution(
            adjustedAttackDistribution,
            character.AITemperature
        );

        return bestAttack;
    }

    public Character ChooseTarget(CharacterAttack attack)
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

    private Dictionary<CharacterAttack, float> GetAttackDistribution()
    {
        Dictionary<CharacterAttack, float> attackDistribution = new();

        foreach (var attack in character.Attacks)
        {
            attackDistribution[attack] = GetAttackPriority(attack);
        }

        return attackDistribution;
    }

    private float GetAttackPriority(CharacterAttack attack)
    {
        float priority = attack.Priority;

        foreach (var condition in attack.PriorityConditions)
        {
            if (IsConditionMet(condition))
            {
                priority += condition.priorityAddition;
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

    private Dictionary<CharacterAttack, float> AdjustAttackDistribution(
        Dictionary<CharacterAttack, float> baseDistribution
    )
    {
        var adjustedDistribution = new Dictionary<CharacterAttack, float>();

        // Calculate time-to-ready for each attack
        var timeToReady = new Dictionary<CharacterAttack, float>();
        var resourceTimeToReady = new Dictionary<CharacterAttack, float>();

        foreach (var attack in baseDistribution.Keys)
        {
            // Calculate cooldown time remaining
            float cooldownTimeRemaining = Mathf.Max(
                0f,
                attack.Cooldown - (Time.time - attack.LastAttackTime)
            );

            // Calculate resource time remaining
            float resourceTimeRemaining = 0f;
            if (attack.ResourceCost > character.CurrentResource)
            {
                float resourceNeeded = attack.ResourceCost - character.CurrentResource;
                resourceTimeRemaining =
                    resourceNeeded / character.ResourceRegen * character.ResourceRegenInterval;
            }

            timeToReady[attack] = Mathf.Max(cooldownTimeRemaining, resourceTimeRemaining);
            resourceTimeToReady[attack] = resourceTimeRemaining;
        }

        // Find the highest priority attack that can't be used due to resources
        float highestBlockedPriority = 0f;
        foreach (var kvp in baseDistribution)
        {
            var attack = kvp.Key;
            var priority = kvp.Value;

            if (resourceTimeToReady[attack] > 0f && priority > highestBlockedPriority)
            {
                highestBlockedPriority = priority;
            }
        }

        // Apply adjustments to each attack
        foreach (var kvp in baseDistribution)
        {
            var attack = kvp.Key;
            var basePriority = kvp.Value;
            var timeUntilReady = timeToReady[attack];

            // Calculate readiness penalty using a continuous function
            // Attacks ready now or in 0.5s get full priority, attacks ready in 2-3 seconds get heavily penalized
            // https://www.wolframalpha.com/input?i=e%5E%28%28x+-+0.5%29+*+-1.33%29+from+0+to+3
            float readinessMultiplier = Mathf.Min(1f, Mathf.Exp(-1.33f * (timeUntilReady - 0.5f)));

            // Calculate resource penalty if we have higher priority attacks that are resource-blocked
            float resourcePenalty = 1f;
            if (attack.ResourceCost > 0 && highestBlockedPriority > basePriority)
            {
                // If this attack uses resources and there's a higher priority attack that's resource-blocked,
                // penalize this attack proportionally to how much higher the blocked priority is
                float priorityGap = highestBlockedPriority - basePriority;
                resourcePenalty = Mathf.Max(0.1f, 1f - priorityGap * 0.5f); // Reduce penalty as gap increases
            }

            float adjustedPriority = basePriority * readinessMultiplier * resourcePenalty;
            adjustedDistribution[attack] = adjustedPriority;
        }

        return adjustedDistribution;
    }
}
