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

    private Transform _target;
    private Vector3 _anchor;
    private Vector3 _followVelocity;

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
    }

    private void ApplyPose()
    {
        transform.SetPositionAndRotation(_anchor + Offset, ViewRotation);
    }
}
