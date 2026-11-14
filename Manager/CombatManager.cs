using SHIN;
using UnityEngine;

/// <summary>
/// 피격의 최종 피해를 정하고, 공격자와 피해자에게 각각 알린다.
/// </summary>
public class CombatManager : ManagerBase
{
    public void JudgeHit(CharacterBase attacker, CharacterBase victim, float rawDamage)
    {
        if (victim == null || victim.Info == null || victim.Info.IsDead)
            return;

        var damage = Mathf.Max(0f, rawDamage);
        if (damage <= 0f)
            return;

        var result = new DamageResult(attacker, victim, damage, damage);
        if (attacker != null && attacker != victim)
            attacker.ReceiveDealtDamage(result);

        victim.ReceiveTakenDamage(result);
    }
}
