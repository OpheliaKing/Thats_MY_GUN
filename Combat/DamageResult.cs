using SHIN;

/// <summary>
/// 한 번의 피격 결과. 준 피해와 받은 피해는 컴뱃 매니저가 정한다.
/// </summary>
public readonly struct DamageResult
{
    public readonly CharacterBase Attacker;
    public readonly CharacterBase Victim;
    public readonly float Dealt;
    public readonly float Received;

    public DamageResult(CharacterBase attacker, CharacterBase victim, float dealt, float received)
    {
        Attacker = attacker;
        Victim = victim;
        Dealt = dealt;
        Received = received;
    }
}
