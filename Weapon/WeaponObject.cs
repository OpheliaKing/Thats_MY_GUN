using System.Threading.Tasks;
using SHIN;
using UnityEngine;

/// <summary>
/// 손에 보이는 무기. 발사 신호가 켜져 있는 동안 WeaponInfo에 시간을 넘기고, 발사 타이밍이면 총구에서 쏜다.
/// </summary>
public class WeaponObject : MonoBehaviour
{
    const string DryFireSeAddress = "Assets/Addressable/Sound/SE/Gun/se_gun_dryfire_001.wav";
    const float GunshotHearRange = 12f;
    const float GunshotGizmoTime = 0.35f;

    static int _hearMask = int.MinValue;
    static readonly Collider[] HearHits = new Collider[16];

    static int HearMask
    {
        get
        {
            if (_hearMask == int.MinValue)
                _hearMask = LayerMask.GetMask(ProjectileDamageObject.UnitLayerName);

            return _hearMask;
        }
    }

    [SerializeField] private Transform _bulletSpawnPoint;
    [SerializeField] private Transform _fireEffectPoint;
    [SerializeField] private string _fireEffectAddress;
    [SerializeField] private string _fireSeAddress;
    [SerializeField] private CameraShakePreset _shakePreset = CameraShakePreset.Rifle;
    [SerializeField] private CameraShakeSettings _shakeCustom = new CameraShakeSettings();

    private WeaponInfo _info;
    private Transform _owner;
    private ProjectilePool _pool;
    private Vector3 _gunshotGizmoOrigin;
    private float _gunshotGizmoUntil;

    public Transform BulletSpawnPoint => _bulletSpawnPoint != null ? _bulletSpawnPoint : transform;
    public Transform FireEffectPoint => _fireEffectPoint != null ? _fireEffectPoint : transform;
    public string FireEffectAddress => _fireEffectAddress;
    public string FireSeAddress => _fireSeAddress;
    public WeaponInfo Info => _info;

    public void Bind(WeaponInfo info, Transform owner, ProjectilePool pool)
    {
        _info = info;
        _owner = owner;
        _pool = pool;
        if (!string.IsNullOrEmpty(_fireEffectAddress))
            _pool?.EnsureEffect(_fireEffectAddress);
    }

    public void SetFireSignal(bool firing)
    {
        if (_info == null)
            return;

        _info.SetFireSignal(firing);
    }

    private void OnDisable()
    {
        _info?.SetFireSignal(false);
        var effects = GetComponentsInChildren<PooledEffect>(true);
        for (var i = 0; i < effects.Length; i++)
            effects[i].Release();
    }

    private void Update()
    {
        if (_info == null)
            return;

        switch (_info.Tick(Time.deltaTime))
        {
            case WeaponFireResult.Fired:
                Fire();
                break;
            case WeaponFireResult.Empty:
                DryFire();
                break;
        }
    }

    private void Fire()
    {
        Debug.Log($"[WeaponObject] 가상 발사. 발사함. 남은 탄약={_info.Ammo}/{MaxMagazine}", this);
        FireProjectile();
        SpawnFireEffect();
        PlayFireSe();
        ShakeCamera();
        NotifyGunshot();
    }

    /// <summary>
    /// 총알이 나간 자리에 총성을 들려 준다. 반응은 몬스터가 정한다.
    /// </summary>
    private void NotifyGunshot()
    {
        var origin = BulletSpawnPoint.position;
        _gunshotGizmoOrigin = origin;
        _gunshotGizmoUntil = Time.time + GunshotGizmoTime;
        var count = Physics.OverlapSphereNonAlloc(origin, GunshotHearRange, HearHits, HearMask, QueryTriggerInteraction.Ignore);
        var shooter = _owner != null ? _owner.GetComponent<CharacterBase>() : null;
        for (var i = 0; i < count; i++)
        {
            var hit = HearHits[i];
            HearHits[i] = null;
            if (hit == null)
                continue;

            var ai = hit.GetComponentInParent<EnemyAI>();
            if (ai == null)
                continue;

            ai.HearGunshot(origin, shooter);
        }
    }

    private void PlayFireSe()
    {
        if (string.IsNullOrEmpty(_fireSeAddress))
            return;

        var sound = GameManager.Instance != null ? GameManager.Instance.SoundManager : null;
        sound?.Play(SoundType.Se, _fireSeAddress);
    }

    private void ShakeCamera()
    {
        var settings = CameraShakeSettings.Resolve(_shakePreset, _shakeCustom);
        if (settings == null || _owner == null)
            return;

        var owner = _owner.GetComponent<CharacterBase>();
        if (owner == null || owner.CharacterType != CharacterType.Player)
            return;

        PlayerCamera.Current?.Shake(settings);
    }

    private async void SpawnFireEffect()
    {
        var address = _fireEffectAddress;
        var pool = _pool;
        if (pool == null || string.IsNullOrEmpty(address))
            return;

        var effect = await pool.GetEffectAsync(address);
        if (effect == null)
            return;

        var point = FireEffectPoint;
        if (point == null || !point.gameObject.activeInHierarchy)
        {
            pool.ReleaseEffect(effect, address);
            return;
        }

        effect.transform.SetParent(point, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.identity;
        effect.SetActive(true);
    }

    private async void FireProjectile()
    {
        var muzzle = BulletSpawnPoint;
        var speed = _info.Data != null ? _info.Data.BulletSpeed : 0f;
        var address = _info.Data != null ? _info.Data.ProjectileAddress : null;
        var direction = AimDirection();
        var rotation = Quaternion.LookRotation(direction, Vector3.up);
        if (_pool == null || string.IsNullOrEmpty(address))
        {
            var bullet = new GameObject("Projectile");
            bullet.transform.SetPositionAndRotation(muzzle.position, rotation);
            var created = bullet.AddComponent<ProjectileDamageObject>();
            created.Launch(_info.Damage, speed, direction, _owner);
            return;
        }

        var projectile = await _pool.GetAsync(address);
        if (projectile == null)
            return;

        direction = AimDirection();
        rotation = Quaternion.LookRotation(direction, Vector3.up);
        projectile.transform.SetPositionAndRotation(muzzle.position, rotation);
        projectile.Launch(_info.Damage, speed, direction, _owner);
    }

    private Vector3 AimDirection()
    {
        var direction = _owner != null ? _owner.forward : BulletSpawnPoint.forward;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    /// <summary>
    /// 탄이 없을 때의 발사. 총알은 나가지 않고 찰칵 소리만 낸다.
    /// </summary>
    private void DryFire()
    {
        var sound = GameManager.Instance != null ? GameManager.Instance.SoundManager : null;
        sound?.Play(SoundType.Se, DryFireSeAddress);
    }

    private int MaxMagazine => _info.Data != null ? _info.Data.MaxMagazine : 0;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || Time.time > _gunshotGizmoUntil)
            return;

        Gizmos.color = new Color(0.35f, 0.85f, 1f, 1f);
        Gizmos.DrawWireSphere(_gunshotGizmoOrigin, GunshotHearRange);
    }
}
