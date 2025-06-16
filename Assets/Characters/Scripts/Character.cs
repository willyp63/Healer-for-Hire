using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    private bool isEnemy = false;
    public bool IsEnemy => isEnemy;

    [SerializeField]
    private int maxHealth = 100;
    public int MaxHealth => maxHealth;

    private int currentHealth = 0;
    private CharacterAttack[] attacks;
    private Character currentTarget;
    private Dictionary<Character, float> threatTable = new();
    private float threatDecayRate = 0.1f;
    private float threatThreshold = 50f;
    private float lastAttackTime;

    protected Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        attacks = GetComponents<CharacterAttack>();
        currentHealth = MaxHealth;
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        while (currentHealth > 0)
        {
            if (currentTarget == null || currentTarget.currentHealth <= 0)
            {
                currentTarget = CharacterManager.Instance.GetOppositeCharacter(this);
            }

            if (currentTarget != null && currentTarget.currentHealth > 0)
            {
                // Check if any attack is ready
                bool attacked = false;
                foreach (var attack in attacks)
                {
                    if (Time.time - lastAttackTime >= attack.Cooldown)
                    {
                        attack.Attack(currentTarget);
                        lastAttackTime = Time.time;
                        attacked = true;
                        break;
                    }
                }

                if (!attacked)
                {
                    // Check threat table for potential target switch
                    UpdateThreatTable();
                    Character highestThreatTarget = GetHighestThreatTarget();
                    if (highestThreatTarget != null && highestThreatTarget != currentTarget)
                    {
                        currentTarget = highestThreatTarget;
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Character died
        animator.SetTrigger("Die");
        yield return new WaitForSeconds(2f);
        CharacterManager.Instance.RemoveCharacter(this);
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

    private Character GetHighestThreatTarget()
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
