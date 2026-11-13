using System;
using UnityEngine;

/// <summary>
/// 캐릭터의 현재 스탯이다. 설계값인 CharacterData로 시작하고, 증감하면 콜백을 알린다.
/// </summary>
public partial class CharacterInfo
{
    private readonly CharacterData _characterData;

    public event Action<float, float> HpChanged;
    public event Action<float, float> MoveSpeedChanged;
    public event Action Died;

    public CharacterData Data => _characterData;
    public float MaxHp => _characterData != null ? _characterData.MaxHp : 0f;
    public float Hp { get; private set; }
    public float MoveSpeed { get; private set; }
    public bool IsDead { get; private set; }

    public CharacterInfo(CharacterData data)
    {
        _characterData = data;
        Hp = Mathf.Max(0f, MaxHp);
        MoveSpeed = Mathf.Max(0f, data != null ? data.MoveSpeed : 0f);
        if (Hp <= 0f)
            IsDead = true;
    }

    public void AddHp(float amount)
    {
        SetHp(Hp + amount);
    }

    public void SetHp(float value)
    {
        if (IsDead)
            return;

        var previous = Hp;
        var next = Mathf.Clamp(value, 0f, Mathf.Max(0f, MaxHp));
        if (Mathf.Approximately(previous, next))
            return;

        Hp = next;
        HpChanged?.Invoke(previous, Hp);
        if (Hp <= 0f)
            Die();
    }

    public void AddMoveSpeed(float amount)
    {
        SetMoveSpeed(MoveSpeed + amount);
    }

    public void SetMoveSpeed(float value)
    {
        var previous = MoveSpeed;
        var next = Mathf.Max(0f, value);
        if (Mathf.Approximately(previous, next))
            return;

        MoveSpeed = next;
        MoveSpeedChanged?.Invoke(previous, MoveSpeed);
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        Hp = 0f;
        Died?.Invoke();
    }
}
