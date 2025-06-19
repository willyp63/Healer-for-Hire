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
    private float aiStrength = 1f; // 0 is very dumb, 1 is very smart
    public float AIStrength => aiStrength;
    public float AITemperature => Mathf.Pow(10, (1f - aiStrength) * 4 - 2); // Maps 0→100, 1→0.01

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
    private float resourceRegenInterval = 1f;
    public float ResourceRegenInterval => resourceRegenInterval;

    [SerializeField]
    private ResourceType resourceType = ResourceType.Mana;
    public ResourceType ResourceType => resourceType;

    private int currentResource = 0;
    public int CurrentResource => currentResource;

    private float lastResourceUpdateTime = Mathf.NegativeInfinity;

    private int currentHealth = 0;
    public int CurrentHealth => currentHealth;

    private bool isDead = false;
    public bool IsDead => isDead;

    private const float GLOBAL_COOLDOWN = 1f;

    private float lastAttackTime = Mathf.NegativeInfinity;

    private bool isCasting = false;
    public bool IsCasting => isCasting;

    private float castStartTime = Mathf.NegativeInfinity;
    public float CastStartTime => castStartTime;

    private Dictionary<Character, float> threatTable = new();
    public Dictionary<Character, float> ThreatTable => threatTable;

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

    private CharacterAttack[] attacks;
    public CharacterAttack[] Attacks => attacks;

    private const float DECISION_INTERVAL = 1.0f;
    private float lastDecisionTime = Mathf.NegativeInfinity;

    private CharacterAttack currentAttack;
    private Character currentTarget;

    private CharacterAI characterAI;
    private Animator animator;

    private void Start()
    {
        characterAI = GetComponent<CharacterAI>();
        animator = GetComponent<Animator>();
        attacks = GetComponents<CharacterAttack>();
        currentHealth = MaxHealth;
        currentResource = StartingResource;

        // give random initial delay to avoid all characters attacking at the same time
        lastAttackTime = Time.time - UnityEngine.Random.Range(0f, GLOBAL_COOLDOWN * 0.5f);
    }

    private void Update()
    {
        if (isDead)
            return;

        UpdateStatusEffects();
        UpdateResource();

        if (isCasting)
        {
            UpdateCast();
        }
        else
        {
            UpdateDecision();
            TryToAttack();
        }
    }

    private void UpdateCast()
    {
        if (Time.time - castStartTime >= currentAttack.CastTime)
        {
            isCasting = false;
            Attack();
        }
    }

    private void UpdateResource()
    {
        // Update resource every ResourceRegenInterval seconds
        if (Time.time - lastResourceUpdateTime <= ResourceRegenInterval)
            return;

        AddResource(resourceRegen);
        lastResourceUpdateTime = Time.time;
    }

    private void UpdateDecision()
    {
        // Update decision every DECISION_INTERVAL seconds
        if (Time.time - lastDecisionTime <= DECISION_INTERVAL)
            return;

        MakeAttackDecision();
    }

    private void TryToAttack()
    {
        // Check if we can attack
        if (
            Time.time - lastAttackTime < GLOBAL_COOLDOWN
            || currentAttack == null
            || currentAttack.IsOnCooldown()
            || currentAttack.ResourceCost > currentResource
        )
            return;

        // Choose a target
        currentTarget = characterAI.ChooseTarget(currentAttack);
        if (currentTarget == null)
            return;

        StartCoroutine(BeginAttack());
    }

    private IEnumerator BeginAttack()
    {
        // add a random delay to avoid attacks from lining up over and over
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));

        if (currentAttack.CastTime > 0)
        {
            isCasting = true;
            castStartTime = Time.time;

            animator.SetTrigger("Cast");
        }
        else
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (currentAttack == null || currentTarget == null)
            return;

        // Consume resource
        AddResource(-currentAttack.ResourceCost);

        // Attack
        currentAttack.Attack(currentTarget);

        // Choose a new attack
        currentAttack = null;
        currentTarget = null;
        MakeAttackDecision();
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

    private void MakeAttackDecision()
    {
        currentAttack = characterAI.ChooseAttack();
        lastDecisionTime = Time.time;
    }

    public void AddResource(int amount)
    {
        if (isDead)
            return;

        currentResource += amount;
        currentResource = Math.Max(Math.Min(currentResource, MaxResource), 0);
    }

    public Character GetHighestThreatTarget()
    {
        var tauntTarget = GetTauntTarget();
        if (tauntTarget != null)
            return tauntTarget;

        Character highestThreatTarget = null;
        float highestThreat = 0f;

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
        if (isDead)
            return;

        if (!threatTable.ContainsKey(source))
        {
            threatTable[source] = 0;
        }
        threatTable[source] += amount;
    }

    public void SetThreat(Character source, float amount)
    {
        if (isDead)
            return;

        threatTable[source] = amount;
    }

    public void Damage(int amount, bool hurt = true)
    {
        if (isDead)
            return;

        // Calculate total damage reduction from all active effects
        float totalReduction = 1f;
        foreach (var effect in activeEffects)
        {
            if (effect.EffectType == StatusEffectType.DamageReduction)
            {
                totalReduction *= 1f - effect.Value;
            }
        }

        // Apply damage reduction
        amount = Mathf.RoundToInt(amount * totalReduction);

        currentHealth -= amount;
        currentHealth = Math.Max(currentHealth, 0);

        if (ResourceType == ResourceType.Rage)
        {
            AddResource(2 * (int)(amount * 100f / MaxHealth));
        }

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            StartCoroutine(Die());
        }
        else if (hurt)
        {
            animator.SetTrigger("Hurt");
        }
    }

    private IEnumerator Die()
    {
        RemoveAllStatusEffects();

        animator.SetTrigger("Die");
        yield return new WaitForSeconds(2f);
        CharacterManager.Instance.RemoveCharacter(this);
    }

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Math.Min(currentHealth, MaxHealth);
    }

    public void ApplyStatusEffect(StatusEffect effect, Character source)
    {
        if (isDead)
            return;

        var existingEffect = activeEffects.Find(e => e.EffectName == effect.EffectName);
        if (existingEffect)
        {
            existingEffect.Initialize(this, source);
            existingEffect.OnApply();
            if (existingEffect.IsStackable)
                existingEffect.AddStack();
            return;
        }

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

    public Character GetTauntTarget()
    {
        var tauntEffect = activeEffects.Find(e => e.EffectType == StatusEffectType.Taunt);
        if (tauntEffect != null)
            return tauntEffect.Source;
        return null;
    }
}
