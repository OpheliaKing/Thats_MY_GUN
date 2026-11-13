using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 날아가는 탄알. 첫 프레임은 주인 몸에서 확인하고, 이후에는 이전 위치에서 현재 위치까지 레이를 쏜다.
/// </summary>
public class ProjectileDamageObject : DamageObject
{
    public const string UnitLayerName = "Unit";
    public const string WallLayerName = "Wall";
    public const string ObstacleLayerName = "Obstacle";

    private static int _hitMask = int.MinValue;

    private static int HitMask
    {
        get
        {
            if (_hitMask == int.MinValue)
                _hitMask = LayerMask.GetMask(UnitLayerName, WallLayerName, ObstacleLayerName);

            return _hitMask;
        }
    }

    private float _speed;
    private Transform _owner;
    private Vector3 _direction;
    private bool _checkedBody;
    private float _lifeTime = 3f;
    private ProjectilePool _pool;
    private string _address;

    [SerializeField] private string _hitEffectAddress;

    public string Address => _address;
    public string HitEffectAddress => _hitEffectAddress;
    public bool InPool { get; private set; }

    public void SetPool(ProjectilePool pool, string address)
    {
        _pool = pool;
        _address = address;
    }

    public void MarkPooled()
    {
        InPool = true;
    }

    public void Launch(float damage, float speed, Vector3 direction, Transform owner)
    {
        _damage = damage;
        _speed = Mathf.Max(0f, speed);
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _owner = owner;
        _checkedBody = false;
        _lifeTime = 3f;
        InPool = false;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_checkedBody)
        {
            _checkedBody = true;
            var body = _owner != null ? _owner.position : transform.position;
            if (TryHit(body, transform.position))
                return;
        }

        if (_speed <= 0f)
        {
            DestroySelf();
            return;
        }

        _lifeTime -= Time.deltaTime;
        if (_lifeTime <= 0f)
        {
            DestroySelf();
            return;
        }

        var previous = transform.position;
        var next = previous + _direction * (_speed * Time.deltaTime);
        if (TryHit(previous, next))
            return;

        transform.position = next;
    }

    private bool TryHit(Vector3 from, Vector3 to)
    {
        var offset = to - from;
        var distance = offset.magnitude;
        if (distance <= 0.0001f)
            return false;

        var hits = Physics.RaycastAll(from, offset / distance, distance, HitMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, CompareDistance);
        for (var i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null || IsOwner(hit.collider.transform))
                continue;

            if (IsSolid(hit.collider.gameObject.layer))
            {
                Debug.Log($"[Projectile] 막힘. target={hit.collider.name}, point={hit.point}", this);
                SpawnHitEffect(hit.point, hit.normal);
                DestroySelf();
                return true;
            }

            Debug.Log($"[Projectile] 명중. target={hit.collider.name}, damage={_damage}, point={hit.point}", this);
            SpawnHitEffect(hit.point, hit.normal);
            DestroySelf();
            return true;
        }

        return false;
    }

    private async void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        var address = _hitEffectAddress;
        var pool = _pool;
        if (pool == null || string.IsNullOrEmpty(address))
            return;

        var effect = await pool.GetEffectAsync(address);
        if (effect == null)
            return;

        var rotation = normal.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        effect.transform.SetPositionAndRotation(point, rotation);
        effect.SetActive(true);
    }

    private void DestroySelf()
    {
        if (_pool != null)
        {
            _pool.Release(this);
            return;
        }

        Destroy(gameObject);
    }

    private bool IsOwner(Transform target)
    {
        return _owner != null && (target == _owner || target.IsChildOf(_owner));
    }

    private static bool IsSolid(int layer)
    {
        return layer == LayerMask.NameToLayer(WallLayerName) || layer == LayerMask.NameToLayer(ObstacleLayerName);
    }

    private static int CompareDistance(RaycastHit a, RaycastHit b)
    {
        return a.distance.CompareTo(b.distance);
    }
}
