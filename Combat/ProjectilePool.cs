using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 프로젝타일 주소마다 대기 탄을 둔다. 판이 있는 동안만 살아 있다.
/// </summary>
public class ProjectilePool
{
    private readonly Dictionary<string, Stack<ProjectileDamageObject>> _pools = new();
    private readonly Dictionary<string, Stack<GameObject>> _effects = new();
    private readonly HashSet<GameObject> _effectsInPool = new();
    private readonly ResourceManager _resources;
    private Transform _root;

    public ProjectilePool(Transform parent, ResourceManager resources)
    {
        _resources = resources;
        var root = new GameObject("ProjectilePool");
        root.transform.SetParent(parent, false);
        _root = root.transform;
    }

    public void Ensure(string address)
    {
        if (string.IsNullOrEmpty(address) || _pools.ContainsKey(address))
            return;

        _pools[address] = new Stack<ProjectileDamageObject>();
        Debug.Log($"[ProjectilePool] 풀 추가. address={address}");
    }

    public async Task<ProjectileDamageObject> GetAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
            return null;

        Ensure(address);
        var stack = _pools[address];
        while (stack.Count > 0)
        {
            var pooled = stack.Pop();
            if (pooled == null)
                continue;

            return pooled;
        }

        if (_resources == null || _root == null)
        {
            Debug.LogError("[ProjectilePool] ResourceManager가 없습니다.");
            return null;
        }

        var instance = await _resources.InstantiateAsync(address, _root, startInactive: true);
        if (instance == null)
            return null;

        var projectile = instance.GetComponent<ProjectileDamageObject>();
        if (projectile == null)
            projectile = instance.AddComponent<ProjectileDamageObject>();

        projectile.SetPool(this, address);
        if (!string.IsNullOrEmpty(projectile.HitEffectAddress))
            EnsureEffect(projectile.HitEffectAddress);
        return projectile;
    }

    public void EnsureEffect(string address)
    {
        if (string.IsNullOrEmpty(address) || _effects.ContainsKey(address))
            return;

        _effects[address] = new Stack<GameObject>();
        Debug.Log($"[ProjectilePool] 이펙트 풀 추가. address={address}");
    }

    public async Task<GameObject> GetEffectAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
            return null;

        EnsureEffect(address);
        var stack = _effects[address];
        while (stack.Count > 0)
        {
            var pooled = stack.Pop();
            if (pooled == null)
                continue;

            _effectsInPool.Remove(pooled);
            var pooledEffect = pooled.GetComponent<PooledEffect>();
            if (pooledEffect != null && pooledEffect.IsReusable)
                return pooled;

            if (pooled.activeSelf)
                continue;

            UnityEngine.Object.Destroy(pooled);
        }

        if (_resources == null || _root == null)
        {
            Debug.LogError("[ProjectilePool] ResourceManager가 없습니다.");
            return null;
        }

        var instance = await _resources.InstantiateAsync(address, _root, startInactive: true);
        if (instance == null)
            return null;

        var effect = instance.GetComponent<PooledEffect>();
        if (effect == null)
            effect = instance.AddComponent<PooledEffect>();

        effect.Bind(this, address);
        return instance;
    }

    public void ReleaseEffect(GameObject effect, string address)
    {
        if (effect == null || _root == null || _effectsInPool.Contains(effect))
            return;

        if (!_effects.TryGetValue(address, out var stack))
        {
            stack = new Stack<GameObject>();
            _effects[address] = stack;
        }

        _effectsInPool.Add(effect);
        effect.SetActive(false);
        effect.transform.SetParent(_root, false);
        stack.Push(effect);
    }

    public void Release(ProjectileDamageObject projectile)
    {
        if (projectile == null || projectile.InPool || _root == null)
            return;

        if (!_pools.TryGetValue(projectile.Address, out var stack))
        {
            stack = new Stack<ProjectileDamageObject>();
            _pools[projectile.Address] = stack;
        }

        projectile.MarkPooled();
        projectile.gameObject.SetActive(false);
        projectile.transform.SetParent(_root, false);
        stack.Push(projectile);
    }

    public void Clear()
    {
        _pools.Clear();
        _effects.Clear();
        _effectsInPool.Clear();
        if (_root == null)
            return;

        UnityEngine.Object.Destroy(_root.gameObject);
        _root = null;
    }
}
