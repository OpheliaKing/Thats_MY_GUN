using UnityEngine;
using UnityEngine.AI;

namespace SHIN
{
    /// <summary>
    /// 아군과 적이 같은 몸에서 행동만 나눈다. 이동과 발사는 캐릭터 의도로만 넘긴다.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        const int SpotJumpCount = 2;
        const float ViewRange = 7f;
        const float ViewAngle = 43f;
        const float FireRange = 9f;
        const float FireAngle = 25f;
        const float EyeHeight = 0.9f;

        static int _sightMask = int.MinValue;

        static int SightMask
        {
            get
            {
                if (_sightMask == int.MinValue)
                    _sightMask = LayerMask.GetMask(
                        ProjectileDamageObject.WallLayerName,
                        ProjectileDamageObject.ObstacleLayerName);

                return _sightMask;
            }
        }

        CharacterBase _self;
        CharacterBase _target;
        NavMeshAgent _agent;
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
                case AiBehavior.Alert:
                    TickAlert();
                    break;
                case AiBehavior.Combat:
                    TickCombat();
                    break;
                case AiBehavior.Idle:
                    TickIdle();
                    if (CanSeeTarget())
                    {
                        SetBehavior(AiBehavior.Alert);
                        _self.PlaySpotJump(SpotJumpCount);
                    }
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

        void TickAlert()
        {
            if (!HasLivingTarget())
            {
                _self.StopSpotJump();
                StopIntent();
                SetBehavior(AiBehavior.Idle);
                return;
            }

            _self.SetMoveDirection(Vector3.zero);
            _self.SetFireHeld(false);
            _self.CharacterRotateToLook(_target.transform.position);
            if (_self.IsSpotJumping)
                return;

            SetBehavior(AiBehavior.Combat);
            TickCombat();
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
            if (toTarget.magnitude > FireRange)
            {
                var direction = FollowDirection();
                _self.SetMoveDirection(direction);
                _self.SetFireHeld(false);
                if (direction.sqrMagnitude > 0.0001f)
                    _self.CharacterRotateToLook(_self.transform.position + direction);
                return;
            }

            StopFollow();
            _self.SetMoveDirection(Vector3.zero);
            _self.CharacterRotateToLook(_target.transform.position);

            var facing = toTarget.sqrMagnitude < 0.0001f
                ? 0f
                : Vector3.Angle(_self.transform.forward, toTarget);
            var fire = facing <= FireAngle;
            _self.SetFireHeld(fire);
        }

        Vector3 FollowDirection()
        {
            var agent = EnsureAgent();
            if (agent == null)
                return Vector3.zero;

            agent.nextPosition = _self.transform.position;
            agent.SetDestination(_target.transform.position);
            var velocity = agent.desiredVelocity;
            velocity.y = 0f;
            return velocity;
        }

        NavMeshAgent EnsureAgent()
        {
            if (_agent == null)
            {
                _agent = _self.GetComponent<NavMeshAgent>();
                if (_agent == null)
                    _agent = _self.gameObject.AddComponent<NavMeshAgent>();

                _agent.updatePosition = false;
                _agent.updateRotation = false;
                _agent.autoBraking = false;
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                _agent.radius = 0.5f;
                _agent.height = 2f;
            }

            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(_self.transform.position, out var hit, 2f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
                var body = _self.GetComponent<Rigidbody>();
                if (body != null)
                    body.position = _self.transform.position;
            }

            return _agent.isOnNavMesh ? _agent : null;
        }

        void StopFollow()
        {
            if (_agent != null && _agent.isOnNavMesh && _agent.hasPath)
                _agent.ResetPath();
        }

        bool CanSeeTarget()
        {
            if (!HasLivingTarget() || !IsHostile(_self.CharacterType, _target.CharacterType))
                return false;

            var origin = _self.transform.position + Vector3.up * EyeHeight;
            var aim = _target.transform.position + Vector3.up * EyeHeight;
            var toTarget = aim - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > ViewRange * ViewRange)
                return false;

            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            if (Vector3.Angle(_self.transform.forward, toTarget) > ViewAngle)
                return false;

            var offset = aim - origin;
            var distance = offset.magnitude;
            if (distance <= 0.0001f)
                return true;

            return !Physics.Raycast(origin, offset / distance, distance, SightMask, QueryTriggerInteraction.Ignore);
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
            StopFollow();
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
