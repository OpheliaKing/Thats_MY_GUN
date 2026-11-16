using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 아군과 적이 같은 몸에서 행동만 나눈다. 이동과 발사는 캐릭터 의도로만 넘긴다.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        const float ViewRange = 7f;
        const float ViewAngle = 43f;
        const float FireRange = 9f;
        const float FireAngle = 25f;

        CharacterBase _self;
        CharacterBase _target;
        AiBehavior _behavior = AiBehavior.Idle;

        public AiBehavior Behavior => _behavior;

        public void Bind(CharacterBase self, CharacterBase target)
        {
            _self = self;
            _target = target;
        }

        public void SetBehavior(AiBehavior behavior)
        {
            _behavior = behavior;
        }

        void LateUpdate()
        {
            if (_self == null)
                return;

            if (_self.Info == null || _self.Info.IsDead)
            {
                StopIntent();
                return;
            }

            switch (_self.CharacterType)
            {
                case CharacterType.Enemy:
                    TickEnemy();
                    break;
                case CharacterType.PlayerFriend:
                    TickAlly();
                    break;
                default:
                    StopIntent();
                    break;
            }
        }

        void TickEnemy()
        {
            switch (_behavior)
            {
                case AiBehavior.Patrol:
                    TickPatrol();
                    break;
                case AiBehavior.Combat:
                    TickCombat();
                    break;
                case AiBehavior.Idle:
                    TickIdle();
                    if (CanSeeTarget())
                        SetBehavior(AiBehavior.Combat);
                    break;
                default:
                    TickIdle();
                    break;
            }
        }

        void TickAlly()
        {
            switch (_behavior)
            {
                case AiBehavior.Follow:
                    TickFollow();
                    break;
                case AiBehavior.Combat:
                    TickCombat();
                    break;
                default:
                    TickIdle();
                    break;
            }
        }

        void TickIdle()
        {
            StopIntent();
        }

        void TickPatrol()
        {
            StopIntent();
        }

        void TickFollow()
        {
            if (!HasLivingTarget())
            {
                StopIntent();
                return;
            }

            _self.SetMoveDirection(Vector3.zero);
            _self.SetFireHeld(false);
            _self.CharacterRotateToLook(_target.transform.position);
        }

        void TickCombat()
        {
            if (!HasLivingTarget() || !IsHostile(_self.CharacterType, _target.CharacterType))
            {
                StopIntent();
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

        bool CanSeeTarget()
        {
            if (!HasLivingTarget() || !IsHostile(_self.CharacterType, _target.CharacterType))
                return false;

            var toTarget = _target.transform.position - _self.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > ViewRange * ViewRange)
                return false;

            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            return Vector3.Angle(_self.transform.forward, toTarget) <= ViewAngle;
        }

        bool HasLivingTarget()
        {
            return _target != null && _target.Info != null && !_target.Info.IsDead;
        }

        static bool IsHostile(CharacterType self, CharacterType target)
        {
            if (self == CharacterType.Enemy)
                return target == CharacterType.Player || target == CharacterType.PlayerFriend;

            if (self == CharacterType.PlayerFriend)
                return target == CharacterType.Enemy;

            return false;
        }

        void StopIntent()
        {
            _self.SetMoveDirection(Vector3.zero);
            _self.SetFireHeld(false);
        }

        void OnDrawGizmos()
        {
            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();
            var origin = transform.position + Vector3.up * 0.15f;
            var left = Quaternion.Euler(0f, -ViewAngle, 0f) * forward;

            Gizmos.color = new Color(1f, 0.82f, 0.2f, 1f);
            const int steps = 24;
            var previous = origin + left * ViewRange;
            Gizmos.DrawLine(origin, previous);
            for (var i = 1; i <= steps; i++)
            {
                var yaw = Mathf.Lerp(-ViewAngle, ViewAngle, i / (float)steps);
                var point = origin + Quaternion.Euler(0f, yaw, 0f) * forward * ViewRange;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.DrawLine(origin, previous);
        }
    }
}
