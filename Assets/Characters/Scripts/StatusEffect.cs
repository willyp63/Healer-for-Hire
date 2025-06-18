using System;
using UnityEngine;

public enum StatusEffectType
{
    // Debuffs
    Stun,
    Taunt,
    Bleed,

    // Buffs
    DamageReduction,
    ResourceRegen,
    HealingOverTime,
}

public class StatusEffect : MonoBehaviour
{
    [SerializeField]
    protected String effectName;
    public String EffectName => effectName;

    [SerializeField]
    protected Sprite effectIcon;
    public Sprite EffectIcon => effectIcon;

    [SerializeField]
    protected StatusEffectType effectType;

    [SerializeField]
    protected float duration;

    [SerializeField]
    protected float tickRate = 1f; // How often the effect applies (for DoTs/HoTs)

    [SerializeField]
    protected float value; // Generic value that can represent damage, healing, etc.
    public float Value => value;

    [SerializeField]
    protected bool isStackable;
    public bool IsStackable => isStackable;

    [SerializeField]
    protected int maxStacks = 1;
    public int MaxStacks => maxStacks;

    protected Character target;
    public Character Target => target;

    protected Character source;
    public Character Source => source;

    protected float timeRemaining;
    protected float nextTickTime;

    protected int currentStacks = 1;
    public int CurrentStacks => currentStacks;

    public StatusEffectType EffectType => effectType;
    public float Duration => duration;
    public float TimeRemaining => timeRemaining;
    public bool IsExpired => timeRemaining <= 0f;

    public virtual void Initialize(Character target, Character source)
    {
        this.target = target;
        this.source = source;
        this.timeRemaining = duration;
        this.nextTickTime = 0f;
        this.currentStacks = 1;
    }

    public virtual void OnApply()
    {
        // Override in derived classes
    }

    public virtual void OnTick()
    {
        // Override in derived classes
    }

    public virtual void OnRemove()
    {
        // Override in derived classes
    }

    public virtual void UpdateEffect()
    {
        timeRemaining -= Time.deltaTime;

        if (tickRate > 0 && Time.time >= nextTickTime)
        {
            OnTick();
            nextTickTime = Time.time + tickRate;
        }
    }

    public virtual void AddStack(int amount = 1)
    {
        currentStacks += amount;
        currentStacks = Math.Min(currentStacks, maxStacks);
    }
}
