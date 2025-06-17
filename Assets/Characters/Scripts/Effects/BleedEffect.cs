using UnityEngine;

public class BleedEffect : StatusEffect
{
    private void Awake()
    {
        effectType = StatusEffectType.Bleed;
    }

    public override void OnApply()
    {
        // Could add visual effects or sound here
    }

    public override void OnTick()
    {
        if (target != null)
        {
            int damage = Mathf.RoundToInt(value) * currentStacks;
            target.Damage(damage, false);
            FloatingTextManager.Instance.SpawnDamage(damage, target.transform.position);
        }
    }

    public override void OnRemove()
    {
        // Clean up any visual effects
    }
}
