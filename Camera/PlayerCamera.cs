using UnityEngine;

/// <summary>
/// 씬의 메인 카메라가 지정된 대상을 따라간다.
/// 대상 위치에 붙여 두지 않고, 이동이 시작되면 느리게 따라가다가 점점 빨라진다.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    private const float OffsetHeight = 12f;
    private const float OffsetBack = 8f;

    // 클수록 출발이 더 느리고, 속도가 붙기까지 더 오래 뒤처진다.
    private const float FollowSmoothTime = 0.55f;

    private static readonly Vector3 Offset = new Vector3(0f, OffsetHeight, -OffsetBack);
    private static readonly Quaternion ViewRotation = Quaternion.LookRotation(-Offset, Vector3.up);

    public static PlayerCamera Current { get; private set; }

    private Transform _target;
    private Vector3 _anchor;
    private Vector3 _followVelocity;
    private float _trauma;
    private float _maxPosition;
    private float _maxRotation;
    private float _frequency = 22f;
    private float _decay = 4f;
    private float _seed;

    private void Awake()
    {
        _seed = UnityEngine.Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    public void Shake(CameraShakeSettings settings)
    {
        if (settings == null || settings.Trauma <= 0f)
            return;

        var stronger = _trauma <= 0.001f || settings.Position >= _maxPosition;
        _trauma = Mathf.Clamp01(_trauma + settings.Trauma);
        if (!stronger)
            return;

        _maxPosition = settings.Position;
        _maxRotation = settings.Rotation;
        _frequency = Mathf.Max(0.01f, settings.Frequency);
        _decay = Mathf.Max(0.01f, settings.Decay);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _followVelocity = Vector3.zero;
        if (_target == null)
            return;

        _anchor = _target.position;
        ApplyPose();
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        _anchor = Vector3.SmoothDamp(_anchor, _target.position, ref _followVelocity, FollowSmoothTime);
        ApplyPose();
        ApplyShake();
    }

    private void ApplyPose()
    {
        transform.SetPositionAndRotation(_anchor + Offset, ViewRotation);
    }

    private void ApplyShake()
    {
        if (_trauma <= 0f)
            return;

        var amount = _trauma * _trauma;
        var time = Time.time * _frequency;
        var sway = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f;
        var bob = (Mathf.PerlinNoise(_seed + 1f, time) - 0.5f) * 2f;
        var pitch = (Mathf.PerlinNoise(_seed + 2f, time) - 0.5f) * 2f;
        var roll = (Mathf.PerlinNoise(_seed + 3f, time) - 0.5f) * 2f;
        transform.position += (transform.right * sway + transform.up * bob) * (_maxPosition * amount);
        transform.rotation *= Quaternion.Euler(pitch * _maxRotation * amount, 0f, roll * _maxRotation * amount);

        _trauma = Mathf.Max(0f, _trauma - _decay * Time.deltaTime);
        if (_trauma <= 0f)
        {
            _maxPosition = 0f;
            _maxRotation = 0f;
        }
    }
}
