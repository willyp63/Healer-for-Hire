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

public enum CharacterState
{
    Idle,
    Moving,
    Casting,
    Dead,
}

public class Character : MonoBehaviour
{
    [SerializeField]
    private Sprite portraitSprite;
    public Sprite PortraitSprite => portraitSprite;

    [SerializeField]
    private bool isMainPlayer = false;
    public bool IsMainPlayer => isMainPlayer;

    [SerializeField]
    private bool isEnemy = false;
    public bool IsEnemy => isEnemy;

    [SerializeField]
    private int enemyPowerLevel = 0;
    public int EnemyPowerLevel => enemyPowerLevel;

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

    private CharacterState state = CharacterState.Idle;
    public CharacterState State => state;

    public bool IsIdle => state == CharacterState.Idle;
    public bool IsMoving => state == CharacterState.Moving;
    public bool IsCasting => state == CharacterState.Casting;
    public bool IsDead => state == CharacterState.Dead;

    private float lastAttackEndTime = Mathf.NegativeInfinity;

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

    private const float HURT_COOLDOWN = 0.25f;
    private float lastHurtTime = Mathf.NegativeInfinity;

    private CharacterAttack currentAttack;
    private Character currentTarget;

    private CharacterAI characterAI;
    private Animator animator;

    private void Awake()
    {
        characterAI = GetComponent<CharacterAI>();
        animator = GetComponent<Animator>();
        attacks = GetComponents<CharacterAttack>();
        currentHealth = MaxHealth;
        currentResource = StartingResource;
    }

    private void Update()
    {
        if (IsDead)
            return;

        UpdateStatusEffects();
        UpdateResource();
        UpdateThreat();

        // Don't attack or make decisions while moving
        if (IsMoving)
            return;

        if (IsCasting)
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
        if (!IsCasting)
            return;

        if (currentAttack == null || currentTarget == null || currentTarget.IsDead)
        {
            SetState(CharacterState.Idle);
            MakeAttackDecision();
            return;
        }

        if (Time.time - castStartTime >= currentAttack.CastTime)
        {
            StartCoroutine(Cast());
        }
    }

    private IEnumerator Cast()
    {
        Attack();

        // HACK: Wait for animator to transition to attack animation before setting state to idle.
        //   otherwise this idle animation plays for a split second before the attack animation
        yield return new WaitForEndOfFrame();
        SetState(CharacterState.Idle);
    }

    private void UpdateResource()
    {
        // Update resource every ResourceRegenInterval seconds
        if (Time.time - lastResourceUpdateTime <= ResourceRegenInterval)
            return;

        AddResource(resourceRegen);
        lastResourceUpdateTime = Time.time;
    }

    private void UpdateThreat()
    {
        var characters = threatTable.Keys.ToList();
        foreach (var character in characters)
        {
            if (character.IsDead)
            {
                threatTable.Remove(character);
            }
        }
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
            Time.time < lastAttackEndTime
            || currentAttack == null
            || currentAttack.IsOnCooldown()
            || currentAttack.ResourceCost > currentResource
        )
            return;

        // Choose a target
        currentTarget = characterAI.ChooseTarget(currentAttack);
        if (currentTarget == null)
            return;

        if (currentAttack.CastTime > 0)
        {
            SetState(CharacterState.Casting);
            castStartTime = Time.time;
        }
        else
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (currentAttack == null || currentTarget == null || currentTarget.IsDead)
        {
            MakeAttackDecision();
            return;
        }

        // Consume resource
        AddResource(-currentAttack.ResourceCost);

        // Attack
        currentAttack.Attack(currentTarget);
        lastAttackEndTime = Time.time + currentAttack.Duration;

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
        if (IsDead)
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
        if (IsDead)
            return;

        if (!threatTable.ContainsKey(source))
        {
            threatTable[source] = 0;
        }
        threatTable[source] += amount;
    }

    public void SetThreat(Character source, float amount)
    {
        if (IsDead)
            return;

        threatTable[source] = amount;
    }

    public void Damage(int amount, bool hurt = true)
    {
        if (IsDead)
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

        if (currentHealth <= 0 && !IsDead)
        {
            SetState(CharacterState.Dead);
            StartCoroutine(Die());
        }
        else if (hurt && Time.time > lastHurtTime + HURT_COOLDOWN)
        {
            lastHurtTime = Time.time;
            animator.SetTrigger("Hurt");
        }
    }

    private IEnumerator Die()
    {
        RemoveAllStatusEffects();

        currentHealth = 0;
        currentResource = 0;

        yield return new WaitForSeconds(1.5f);
        CharacterManager.Instance.RemoveCharacter(this);
    }

    public void Heal(int amount)
    {
        if (IsDead)
            return;

        currentHealth += amount;
        currentHealth = Math.Min(currentHealth, MaxHealth);
    }

    public void ApplyStatusEffect(StatusEffect effect, Character source)
    {
        if (IsDead)
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

    public void SetState(CharacterState newState)
    {
        state = newState;

        Debug.Log($"Setting state to {newState}");

        animator.SetBool("IsMoving", IsMoving && !isEnemy);
        animator.SetBool("IsCasting", IsCasting);
        animator.SetBool("IsDead", IsDead);

        if (newState == CharacterState.Idle)
        {
            // give random initial delay to avoid all characters attacking at the same time
            lastAttackEndTime = Time.time + UnityEngine.Random.Range(1f, 2f);
        }
    }
}
