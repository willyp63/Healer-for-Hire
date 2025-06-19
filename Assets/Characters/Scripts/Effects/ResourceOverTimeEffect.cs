using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceOverTimeEffect : StatusEffect
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
            target.AddResource(Mathf.RoundToInt(value));
        }
    }

    public override void OnRemove()
    {
        // Clean up any visual effects
    }
}
