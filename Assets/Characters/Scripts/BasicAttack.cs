using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicAttack : CharacterAttack
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

        if (target == null)
            yield break;

        CharacterAttack.ApplyDamageAndThreat(target, character, this);
    }
}
