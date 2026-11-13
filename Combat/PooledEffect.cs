using UnityEngine;

/// <summary>
/// 풀에서 꺼낸 일회성 이펙트. 재생이 끝나면 풀로 돌아간다.
/// </summary>
public class PooledEffect : MonoBehaviour
{
    private ProjectilePool _pool;
    private string _address;
    private float _remain;
    private bool _restart;
    private bool _sawParticles;

    public bool IsReusable
    {
        get
        {
            if (gameObject.activeSelf)
                return false;

            if (_sawParticles && GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                return false;

            return true;
        }
    }

    public void Bind(ProjectilePool pool, string address)
    {
        _pool = pool;
        _address = address;
        Disarm();
    }

    public void Release()
    {
        _pool?.ReleaseEffect(gameObject, _address);
    }

    private void OnEnable()
    {
        Disarm();
        _restart = true;
        _remain = 1f;
    }

    /// <summary>
    /// 재생이 끝나면 Stop Action Callback이 이펙트 자식을 파괴한다. 풀에는 빈 오브젝트만 남지 않게 막는다.
    /// </summary>
    private void Disarm()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        for (var i = 0; i < systems.Length; i++)
        {
            var main = systems[i].main;
            main.stopAction = ParticleSystemStopAction.None;
        }

        var callbacks = GetComponentsInChildren<Hovl.HS_CallBackParent>(true);
        for (var i = 0; i < callbacks.Length; i++)
            callbacks[i].enabled = false;
    }

    private float Restart()
    {
        var systems = GetComponentsInChildren<ParticleSystem>(true);
        _sawParticles = systems.Length > 0;
        var life = 0.2f;
        for (var i = 0; i < systems.Length; i++)
        {
            var system = systems[i];
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.Play(true);
            var main = system.main;
            var duration = main.duration + main.startLifetime.constantMax;
            if (duration > life)
                life = duration;
        }

        return life;
    }

    private void Update()
    {
        if (_restart)
        {
            _restart = false;
            _remain = Restart();
        }

        _remain -= Time.deltaTime;
        if (_remain > 0f || _pool == null)
            return;

        _pool.ReleaseEffect(gameObject, _address);
    }
}
