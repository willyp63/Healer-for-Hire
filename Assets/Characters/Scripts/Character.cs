using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CharacterRole
{
    Tank,
    Damage,
    Healer,
}

public class Character : MonoBehaviour
{
    [SerializeField]
    private bool isEnemy = false;
    public bool IsEnemy => isEnemy;

    [SerializeField]
    private int enemyPowerLevel = 0;
    public int EnemyPowerLevel => enemyPowerLevel;

    [SerializeField]
    private CharacterRole role = CharacterRole.Damage;
    public CharacterRole Role => role;

    [SerializeField]
    private float intelligence = 0f;
    public float Intelligence => intelligence;

    [SerializeField]
    private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    private int currentHealth = 0;
    public int CurrentHealth => currentHealth;

    private const float threatDecayRate = 0.1f;
    private const float threatThreshold = 50f;
    private const float globalCooldown = 1f;

    private float lastActionTime = Mathf.NegativeInfinity;

    private Dictionary<Character, float> threatTable = new();
    public Dictionary<Character, float> ThreatTable => threatTable;

    protected Animator animator;
    private CharacterAttack[] attacks;

    private void Start()
    {
        animator = GetComponent<Animator>();
        attacks = GetComponents<CharacterAttack>();
        currentHealth = MaxHealth;
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        yield return new WaitForSeconds(0.1f);

        while (currentHealth > 0)
        {
            UpdateThreatTable();
            TryToAttack();
            yield return new WaitForSeconds(0.1f);
        }

        // Character died
        animator.SetTrigger("Die");
        yield return new WaitForSeconds(2f);
        CharacterManager.Instance.RemoveCharacter(this);
    }

    private void TryToAttack()
    {
        if (Time.time - lastActionTime < globalCooldown)
            return;

        var attackDistribution = GetAttackDistribution();
        var bestAttack = DistributionUtils.SampleDistribution(
            attackDistribution,
            Mathf.Max(1f - Intelligence, 0.1f)
        );

        if (bestAttack == null)
            return;

        bestAttack.Attack();
        lastActionTime = Time.time;
    }

    private Dictionary<CharacterAttack, float> GetAttackDistribution()
    {
        Dictionary<CharacterAttack, float> attackDistribution = new();

        foreach (var attack in attacks)
        {
            if (attack.IsOnCooldown())
                continue;

            attackDistribution[attack] = attack.GetAttackEffectiveness();
        }

        return attackDistribution;
    }

    private void UpdateThreatTable()
    {
        // Create a copy of the keys to safely iterate
        var keys = new List<Character>(threatTable.Keys);

        // Decay threat over time
        foreach (var key in keys)
        {
            threatTable[key] -= threatDecayRate;
            if (threatTable[key] <= 0)
            {
                threatTable.Remove(key);
            }
        }
    }

    public Character GetHighestThreatTarget()
    {
        Character highestThreatTarget = null;
        float highestThreat = threatThreshold;

        foreach (var kvp in threatTable)
        {
            if (kvp.Value > highestThreat)
            {
                highestThreat = kvp.Value;
                highestThreatTarget = kvp.Key;
            }
        }

        return highestThreatTarget;
    }

    public void AddThreat(Character source, float amount)
    {
        if (!threatTable.ContainsKey(source))
        {
            threatTable[source] = 0;
        }
        threatTable[source] += amount;
    }

    public void Damage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Math.Max(currentHealth, 0);

        animator.SetTrigger("Hurt");
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Math.Min(currentHealth, MaxHealth);
    }
}
