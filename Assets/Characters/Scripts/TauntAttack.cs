using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TauntAttack : CharacterAttack
{
    public override void Attack(Character target)
    {
        base.Attack(target);

        StartCoroutine(PerformAttack(target));
    }

    private IEnumerator PerformAttack(Character target)
    {
        if (target == null)
            yield break;

        // Wait for animation to reach the right point
        yield return new WaitForSeconds(animationDelay);

        // Find the highest threat value in the target's threat table
        float highestThreat = 0f;
        foreach (var threatValue in target.ThreatTable.Values)
        {
            if (threatValue > highestThreat)
            {
                highestThreat = threatValue;
            }
        }

        // Set our threat equal to the highest threat value
        if (highestThreat > 0f)
        {
            target.SetThreat(character, highestThreat + 1f);
        }

        CharacterAttack.ApplyDamageAndThreat(target, character, this);

        FloatingTextManager.Instance.SpawnEffect("Taunt", target.transform.position);
    }
}
