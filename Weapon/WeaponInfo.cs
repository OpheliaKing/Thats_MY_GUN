using UnityEngine;

/// <summary>
/// 획득한 무기의 현재 상태. 발사 신호는 켜고 끌 때만 받고, 연사와 탄약은 여기서 판단한다.
/// </summary>
public class WeaponInfo
{
    private bool _fireSignal;
    private float _cooldown;

    public WeaponInfo(WeaponData data)
    {
        if (data == null)
        {
            Debug.LogError("[WeaponInfo] WeaponData가 없습니다.");
            return;
        }

        Data = data;
        Ammo = Mathf.Max(0, data.MaxMagazine);
    }

    public WeaponData Data { get; }
    public int Ammo { get; private set; }
    public bool FireSignal => _fireSignal;

    public float FireRate => Data != null ? Data.FireRate : 0f;
    public float Damage => Data != null ? Data.Damage : 0f;

    public void SetFireSignal(bool firing)
    {
        _fireSignal = firing;
    }

    /// <summary>
    /// 신호가 켜져 있고 연사 간격이 맞으면 한 발을 판단한다. 탄이 없으면 빈 발사다.
    /// </summary>
    public WeaponFireResult Tick(float deltaTime)
    {
        if (!_fireSignal || Data == null || FireRate <= 0f)
            return WeaponFireResult.None;

        _cooldown -= deltaTime;
        if (_cooldown > 0f)
            return WeaponFireResult.None;

        _cooldown += 1f / FireRate;
        if (Ammo <= 0)
            return WeaponFireResult.Empty;

        Ammo--;
        return WeaponFireResult.Fired;
    }
}

public enum WeaponFireResult
{
    None,
    Fired,
    Empty
}
