using System;
using UnityEngine;

/// <summary>
/// 1 소총, 2 대구경, 3 폭발. 커스텀이면 무기에 넣은 수치를 쓴다.
/// </summary>
public enum CameraShakePreset
{
    [InspectorName("없음")]
    None = 0,

    [InspectorName("1 소총")]
    Rifle = 1,

    [InspectorName("2 대구경")]
    Heavy = 2,

    [InspectorName("3 폭발")]
    Explosion = 3,

    [InspectorName("커스텀")]
    Custom = 4,
}

/// <summary>
/// 한 발에 더해지는 카메라 흔들림. 강도는 0~1이고, 위치는 미터, 회전은 도다.
/// </summary>
[Serializable]
public class CameraShakeSettings
{
    [InspectorName("강도")]
    [Tooltip("한 발마다 더해지는 흔들림. 1이면 최대.")]
    [Range(0f, 1f)]
    [SerializeField] private float _trauma = 0.55f;

    [InspectorName("위치")]
    [Tooltip("강도가 최대일 때 화면이 밀리는 거리(m).")]
    [Min(0f)]
    [SerializeField] private float _position = 0.24f;

    [InspectorName("회전")]
    [Tooltip("강도가 최대일 때 카메라가 기울어지는 각도.")]
    [Min(0f)]
    [SerializeField] private float _rotation = 1.2f;

    [InspectorName("주파수")]
    [Tooltip("1초에 흔들리는 횟수.")]
    [Min(0f)]
    [SerializeField] private float _frequency = 20f;

    [InspectorName("감쇠")]
    [Tooltip("1초마다 줄어드는 강도.")]
    [Min(0f)]
    [SerializeField] private float _decay = 3.5f;

    public float Trauma => _trauma;
    public float Position => _position;
    public float Rotation => _rotation;
    public float Frequency => _frequency;
    public float Decay => _decay;

    public CameraShakeSettings()
    {
    }

    public CameraShakeSettings(float trauma, float position, float rotation, float frequency, float decay)
    {
        _trauma = trauma;
        _position = position;
        _rotation = rotation;
        _frequency = frequency;
        _decay = decay;
    }

    public static CameraShakeSettings Resolve(CameraShakePreset preset, CameraShakeSettings custom)
    {
        switch (preset)
        {
            case CameraShakePreset.None:
                return null;
            case CameraShakePreset.Heavy:
                return Heavy;
            case CameraShakePreset.Explosion:
                return Explosion;
            case CameraShakePreset.Custom:
                return custom;
            default:
                return Rifle;
        }
    }

    public static readonly CameraShakeSettings Rifle = new CameraShakeSettings(0.55f, 0.24f, 1.2f, 20f, 3.5f);
    public static readonly CameraShakeSettings Heavy = new CameraShakeSettings(0.72f, 0.38f, 2f, 14f, 2f);
    public static readonly CameraShakeSettings Explosion = new CameraShakeSettings(1f, 0.8f, 4.5f, 8f, 1f);
}
