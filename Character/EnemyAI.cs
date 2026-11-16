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
        const float LoseSightTime = 2f;
        const float SearchArriveDistance = 0.75f;

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
        float _lostSightTimer;
        Vector3 _lastSeenPosition;
        bool _hasLastSeen;
        bool _missJumpPlayed;
        bool _gunshotJump;

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

        /// <summary>
        /// 총성 위치를 듣는다. 보이는 채로 싸우는 중이면 무시하고, 아니면 그 지점을 수색한다.
        /// </summary>
        public void HearGunshot(Vector3 shotPosition, CharacterBase shooter)
        {
            if (_self == null || _self.Info == null || _self.Info.IsDead)
                return;

            if (shooter == null || shooter == _self || !IsHostile(_self.CharacterType, shooter.CharacterType))
                return;

            if (_behavior == AiBehavior.Alert)
                return;

            if (_behavior == AiBehavior.Combat && HasCombatSight())
                return;

            if (_behavior == AiBehavior.Search && _gunshotJump)
            {
                _lastSeenPosition = shotPosition;
                _hasLastSeen = true;
                return;
            }

            _lastSeenPosition = shotPosition;
            _hasLastSeen = true;
            _lostSightTimer = 0f;
            var surprise = _behavior == AiBehavior.Idle || _behavior == AiBehavior.Patrol || _behavior == AiBehavior.Follow;
            _missJumpPlayed = false;
            if (surprise)
            {
                _gunshotJump = true;
                _self.CharacterRotateToLook(shotPosition);
                _self.PlaySpotJump(1);
            }
            else
            {
                _gunshotJump = false;
                _self.StopSpotJump();
            }

            SetBehavior(AiBehavior.Search);
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
                case AiBehavior.Search:
                    TickSearch();
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
                case AiBehavior.Search:
                    TickSearch();
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

            if (HasCombatSight())
            {
                _lostSightTimer = 0f;
                _lastSeenPosition = _target.transform.position;
                _hasLastSeen = true;
                StopFollow();
                _self.SetMoveDirection(Vector3.zero);
                _self.CharacterRotateToLook(_target.transform.position);

                var toTarget = _target.transform.position - _self.transform.position;
                toTarget.y = 0f;
                var facing = toTarget.sqrMagnitude < 0.0001f
                    ? 0f
                    : Vector3.Angle(_self.transform.forward, toTarget);
                _self.SetFireHeld(toTarget.magnitude <= FireRange && facing <= FireAngle);
                return;
            }

            _self.SetFireHeld(false);
            _lostSightTimer += Time.deltaTime;
            if (_lostSightTimer >= LoseSightTime)
            {
                _missJumpPlayed = false;
                SetBehavior(AiBehavior.Search);
                TickSearch();
                return;
            }

            var direction = FollowDirection(_target.transform.position);
            _self.SetMoveDirection(direction);
            if (direction.sqrMagnitude > 0.0001f)
                _self.CharacterRotateToLook(_self.transform.position + direction);
        }

        void TickSearch()
        {
            if (!HasLivingTarget() || !IsHostile(_self.CharacterType, _target.CharacterType))
            {
                StopIntent();
                SetBehavior(AiBehavior.Idle);
                return;
            }

            if (_gunshotJump)
            {
                _self.SetFireHeld(false);
                StopFollow();
                _self.SetMoveDirection(Vector3.zero);
                if (_hasLastSeen)
                    _self.CharacterRotateToLook(_lastSeenPosition);

                if (_self.IsSpotJumping)
                    return;

                _gunshotJump = false;
            }

            if (HasCombatSight())
            {
                _lostSightTimer = 0f;
                _lastSeenPosition = _target.transform.position;
                _hasLastSeen = true;
                _missJumpPlayed = false;
                _self.StopSpotJump();
                SetBehavior(AiBehavior.Combat);
                TickCombat();
                return;
            }

            _self.SetFireHeld(false);
            if (!_hasLastSeen)
            {
                StopIntent();
                SetBehavior(AiBehavior.Idle);
                return;
            }

            var toLastSeen = _lastSeenPosition - _self.transform.position;
            toLastSeen.y = 0f;
            if (toLastSeen.sqrMagnitude <= SearchArriveDistance * SearchArriveDistance)
            {
                if (!_missJumpPlayed)
                {
                    _missJumpPlayed = true;
                    StopFollow();
                    _self.SetMoveDirection(Vector3.zero);
                    _self.SetFireHeld(false);
                    _self.PlaySpotJump(1);
                    return;
                }

                if (_self.IsSpotJumping)
                    return;

                _missJumpPlayed = false;
                StopIntent();
                SetBehavior(AiBehavior.Idle);
                return;
            }

            var direction = FollowDirection(_lastSeenPosition);
            _self.SetMoveDirection(direction);
            if (direction.sqrMagnitude > 0.0001f)
                _self.CharacterRotateToLook(_self.transform.position + direction);
        }

        Vector3 FollowDirection(Vector3 destination)
        {
            var agent = EnsureAgent();
            if (agent == null)
                return Vector3.zero;

            agent.nextPosition = _self.transform.position;
            agent.SetDestination(destination);
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
            if (!HasCombatSight())
                return false;

            var toTarget = _target.transform.position - _self.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            return Vector3.Angle(_self.transform.forward, toTarget) <= ViewAngle;
        }

        bool HasCombatSight()
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
            _gunshotJump = false;
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
