using UnityEngine;

/// <summary>
/// 손 아래 무기. 총알과 이펙트가 나올 위치, 이 무기의 설계값을 가진다.
/// </summary>
public class WeaponObject : MonoBehaviour
{
    [SerializeField] private WeaponInfo _info = new();
    [SerializeField] private Transform _bulletSpawnPoint;
    [SerializeField] private Transform _fireEffectPoint;

    public WeaponInfo Info => _info;

    public Transform BulletSpawnPoint => _bulletSpawnPoint != null ? _bulletSpawnPoint : transform;
    public Transform FireEffectPoint => _fireEffectPoint != null ? _fireEffectPoint : transform;
}
