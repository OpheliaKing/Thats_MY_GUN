using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 적을 플레이어 쪽으로 돌리고, 정면의 사거리 안이면 발사 신호만 켠다.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        const float FireRange = 18f;
        const float FireAngle = 25f;

        CharacterBase _self;
        CharacterBase _target;

        public void Bind(CharacterBase self, CharacterBase target)
        {
            _self = self;
            _target = target;
        }

        void LateUpdate()
        {
            if (_self == null)
                return;

            if (_self.Info == null || _self.Info.IsDead || _target == null || _target.Info == null || _target.Info.IsDead)
            {
                _self.SetMoveDirection(Vector3.zero);
                _self.SetFireHeld(false);
                return;
            }

            var toTarget = _target.transform.position - _self.transform.position;
            toTarget.y = 0f;
            _self.SetMoveDirection(Vector3.zero);
            _self.CharacterRotateToLook(_target.transform.position);

            var facing = toTarget.sqrMagnitude < 0.0001f
                ? 0f
                : Vector3.Angle(_self.transform.forward, toTarget);
            var fire = toTarget.magnitude <= FireRange && facing <= FireAngle;
            _self.SetFireHeld(fire);
        }
    }
}
