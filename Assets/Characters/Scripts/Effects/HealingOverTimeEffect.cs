using UnityEngine;

public class HealingOverTimeEffect : StatusEffect
{
    private void Awake()
    {
        effectType = StatusEffectType.HealingOverTime;
    }

    public override void OnApply()
    {
        // Could add healing visual effects
    }

    public override void OnTick()
    {
        if (target != null)
        {
            target.Heal(Mathf.RoundToInt(value));
        }
    }

    public override void OnRemove()
    {
        // Clean up any visual effects
    }
}
