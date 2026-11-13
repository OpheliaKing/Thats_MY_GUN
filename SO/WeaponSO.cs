using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "CityGun/Weapon")]
public class WeaponSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<string, WeaponData> _weapons = new();

    public WeaponData GetByTid(string tid)
    {
        if (TryGetByTid(tid, out var data))
            return data;

        Debug.LogError($"[WeaponSO] Tid가 없습니다: {tid}", this);
        return null;
    }

    public bool TryGetByTid(string tid, out WeaponData data)
    {
        return _weapons.TryGetValue(tid, out data);
    }
}

/// <summary>
/// 총 발사의 설계값. 연사력은 초당 발사 수다.
/// </summary>
[System.Serializable]
public class WeaponData
{
    [SerializeField] private string _prefabAddress;
    [SerializeField] private string _projectileAddress;
    [SerializeField] private int _maxMagazine = 30;
    [SerializeField] private float _bulletSpeed = 40f;
    [SerializeField] private float _fireRate = 5f;
    [SerializeField] private float _damage = 10f;

    public string PrefabAddress => _prefabAddress;
    public string ProjectileAddress => _projectileAddress;
    public int MaxMagazine => _maxMagazine;
    public float BulletSpeed => _bulletSpeed;
    public float FireRate => _fireRate;
    public float Damage => _damage;
}
