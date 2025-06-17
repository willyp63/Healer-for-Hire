using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CharacterRole
{
    Tank,
    Damage,
    Healer,
}

public enum ResourceType
{
    Mana,
    Rage,
    Energy,
}

public class Character : MonoBehaviour
{
    [SerializeField]
    private Sprite portraitSprite;
    public Sprite PortraitSprite => portraitSprite;

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

    [SerializeField]
    private int maxResource = 100;
    public int MaxResource => maxResource;

    [SerializeField]
    private int startingResource = 100;
    public int StartingResource => startingResource;

    [SerializeField]
    private int resourceRegen = 10;
    public int ResourceRegen => resourceRegen;

    [SerializeField]
    private ResourceType resourceType = ResourceType.Mana;
    public ResourceType ResourceType => resourceType;

    private int currentResource = 0;
    public int CurrentResource => currentResource;

    private int currentHealth = 0;
    public int CurrentHealth => currentHealth;

    private const float threatDecayRate = 0.1f;
    private const float threatThreshold = 50f;
    private const float globalCooldown = 1f;

    private float lastActionTime = Mathf.NegativeInfinity;

    private Dictionary<Character, float> threatTable = new();
    public Dictionary<Character, float> ThreatTable => threatTable;

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

    private CharacterAttack[] attacks;
    public CharacterAttack[] Attacks => attacks;

    private CharacterAI characterAI;
    private Animator animator;

    private void Start()
    {
        characterAI = GetComponent<CharacterAI>();
        animator = GetComponent<Animator>();
        attacks = GetComponents<CharacterAttack>();
        currentHealth = MaxHealth;
        currentResource = StartingResource;
        StartCoroutine(BattleLoop());
        StartCoroutine(ResourceUpdate());
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

    private IEnumerator ResourceUpdate()
    {
        while (currentHealth > 0)
        {
            UpdateResource();
            yield return new WaitForSeconds(1f);
        }
    }

    private void TryToAttack()
    {
        if (Time.time - lastActionTime < globalCooldown)
            return;

        // TODO: implement random decision making when there is no AI
        if (characterAI == null)
            return;

        var (attack, target) = characterAI.MakeDecision();

        if (attack == null)
            return;

        attack.Attack(target);
        lastActionTime = Time.time;
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

    public void AddResource(int amount)
    {
        currentResource += amount;
        currentResource = Math.Min(currentResource, MaxResource);
    }

    private void UpdateResource()
    {
        currentResource += resourceRegen;
        currentResource = Math.Min(currentResource, MaxResource);
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

    public void Damage(int amount, bool hurt = true)
    {
        currentHealth -= amount;
        currentHealth = Math.Max(currentHealth, 0);

        if (ResourceType == ResourceType.Rage)
        {
            AddResource(2 * (int)(amount * 100f / MaxHealth));
        }

        if (hurt)
        {
            animator.SetTrigger("Hurt");
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Math.Min(currentHealth, MaxHealth);
    }

    private void Update()
    {
        UpdateStatusEffects();
    }

    private void UpdateStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.UpdateEffect();

            if (effect.IsExpired)
            {
                effect.OnRemove();
                Destroy(effect.gameObject);
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void ApplyStatusEffect(StatusEffect effect, Character source)
    {
        var existingEffect = activeEffects.Find(e => e.EffectName == effect.EffectName);
        if (existingEffect)
        {
            if (existingEffect.IsStackable)
                existingEffect.AddStack();
            existingEffect.RefreshDuration();
            effect.OnApply();
            return;
        }

        // Check for immunities or resistances here if needed
        effect.Initialize(this, source);
        effect.OnApply();
        activeEffects.Add(effect);
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return activeEffects.Exists(effect => effect.EffectType == type);
    }

    public void RemoveStatusEffect(StatusEffectType type)
    {
        var effect = activeEffects.Find(e => e.EffectType == type);
        if (effect != null)
        {
            effect.OnRemove();
            activeEffects.Remove(effect);
            Destroy(effect.gameObject);
        }
    }

    public void RemoveAllStatusEffects()
    {
        foreach (var effect in activeEffects)
        {
            effect.OnRemove();
            Destroy(effect.gameObject);
        }
        activeEffects.Clear();
    }
}
