using UnityEngine;

/// <summary>
/// 무기의 설계값. 연사율은 초당 발사 수다.
/// </summary>
[System.Serializable]
public class WeaponInfo
{
    [SerializeField] private float _fireRate = 5f;
    [SerializeField] private float _damage = 10f;

    public float FireRate => _fireRate;
    public float Damage => _damage;
}
