using UnityEngine;

namespace SHIN
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public partial class CharacterBase : MonoBehaviour
    {
        private const float MoveSpeed = 5f;
        private const float RotateSpeed = 1080f;

        private const string TopLayerName = "Top";
        private const string WalkSeAddress = "Assets/Addressable/Sound/SE/Walk/se_walk_001.ogg";
        private const float WalkStepDistance = 2f;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveAmountHash = Animator.StringToHash("MoveAmount");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private readonly CharacterStateMachine _stateMachine = new CharacterStateMachine();

        private Rigidbody _rigidbody;
        private Animator _animator;
        private CharacterInfo _info;
        private CharacterAppearance _appearance;
        private CharacterType _characterType = CharacterType.None;
        private Vector3 _moveDirection;
        private Vector2 _moveInput;
        private bool _hasMoveInput;
        private InputManager _input;
        private int _topLayerIndex = -1;
        private float _walkStepTimer;

        private void Reset()
        {
            InitRigidbody();
        }

        private void InitRigidbody()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                return;

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.linearDamping = 0f;
            body.angularDamping = 0f;

            var collider = GetComponent<CapsuleCollider>();
            if (collider != null)
                collider.sharedMaterial = SlideMaterial;
        }

        private static PhysicsMaterial _slideMaterial;

        /// <summary>
        /// 마찰을 없애 벽에 붙어도 옆 방향으로 미끄러지게 한다.
        /// </summary>
        private static PhysicsMaterial SlideMaterial
        {
            get
            {
                if (_slideMaterial != null)
                    return _slideMaterial;

                _slideMaterial = new PhysicsMaterial("CharacterSlide")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };
                return _slideMaterial;
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError("[CharacterBase] Rigidbody가 없습니다.", this);
                return;
            }

            if (GetComponentInChildren<Collider>() == null)
                Debug.LogWarning("[CharacterBase] Collider가 없습니다.", this);

            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
                Debug.LogWarning("[CharacterBase] Animator가 없습니다.", this);
        }

        public CharacterType CharacterType => _characterType;
        public CharacterInfo Info => _info;

        public void InitCharacter(CharacterType characterType, CharacterData data)
        {
            _characterType = characterType;
            if (_info != null)
                _info.Died -= OnDied;

            _info = new CharacterInfo(data);
            _info.Died += OnDied;
            var unitLayer = LayerMask.NameToLayer("Unit");
            if (unitLayer >= 0)
                gameObject.layer = unitLayer;
            CacheAppearance();
            InitRigidbody();
            ResetDeathDissolve();
            ResetHitFlash();
            if (_info.IsDead)
                OnDied();
            else
                ChangeState(CharacterStateId.Idle);
        }

        public void SetAppearance(CharacterPreset preset)
        {
            CacheAppearance();
            if (_appearance == null)
                return;

            _appearance.Apply(preset);
            PrepareEnemyDeathDissolve();
        }

        public void SetAppearance(string headName, string bodyName)
        {
            CacheAppearance();
            if (_appearance == null)
                return;

            _appearance.ApplyCustom(headName, bodyName);
            PrepareEnemyDeathDissolve();
        }

        private void CacheAppearance()
        {
            if (_appearance != null)
                return;

            _appearance = GetComponentInChildren<CharacterAppearance>(true);
            if (_appearance == null)
                Debug.LogWarning("[CharacterBase] CharacterAppearance가 없습니다.", this);
        }

        private void OnDied()
        {
            StopMoveVelocity();
            ChangeState(CharacterStateId.Die);
            if (_animator != null)
                _animator.CrossFade(DieHash, 0.1f);
            PlayEnemyDeathDissolve();
        }

        private void Update()
        {
            _stateMachine.Update();
            ApplyMoveBlend();
            ApplyShootLayer();
            UpdateHitLayer();
            PlayWalkStep();
        }

        private void FixedUpdate()
        {
            _stateMachine.FixedUpdate();
        }

        private void LateUpdate()
        {
            if (_characterType == CharacterType.Player)
                RotatePlayerTowardPointer();
        }

        /// <summary>
        /// 이 인스턴스만 플레이어 입력을 받는다. 스폰한 플레이어에게만 호출한다.
        /// </summary>
        public void BindInput(InputManager input)
        {
            UnbindInput();
            if (input == null)
                return;

            _input = input;
            _input.MoveChanged += OnMoveInput;
            BindFireInput();
            OnMoveInput(_input.Move);
        }

        public void UnbindInput()
        {
            if (_input == null)
                return;

            _input.MoveChanged -= OnMoveInput;
            UnbindFireInput();
            _input = null;
            _hasMoveInput = false;
            _moveInput = Vector2.zero;
            ApplyMoveBlend();
        }

        private void OnDestroy()
        {
            if (_info != null)
                _info.Died -= OnDied;

            UnbindInput();
            ReleaseEnemyDeathDissolve();
            ResetHitFlash();
        }

        private void OnMoveInput(Vector2 move)
        {
            _moveInput = move;
            _hasMoveInput = true;
            SetMoveDirection(ToCameraPlanarDirection(move));
        }

        /// <summary>
        /// 스틱 입력을 카메라의 수평 앞·오른쪽으로 돌린다.
        /// 카메라가 없으면 월드 XZ를 쓴다.
        /// </summary>
        private static Vector3 ToCameraPlanarDirection(Vector2 move)
        {
            var direction = ToCameraPlanarVector(move);
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return direction.normalized;
        }

        private static Vector3 ToCameraPlanarVector(Vector2 move)
        {
            var planar = new Vector3(move.x, 0f, move.y);
            var camera = Camera.main;
            if (camera == null)
                return planar;

            var yaw = Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f);
            var direction = yaw * planar;
            direction.y = 0f;
            return direction;
        }

        /// <summary>
        /// 이동 입력을 캐릭터의 오른쪽(MoveX), 앞(MoveY)으로 넣어 블렌드한다.
        /// </summary>
        private void ApplyMoveBlend()
        {
            if (_animator == null)
                return;

            var move = _hasMoveInput ? _moveInput : Vector2.zero;
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            var local = transform.InverseTransformDirection(ToCameraPlanarVector(move));
            _animator.SetFloat(MoveXHash, local.x);
            _animator.SetFloat(MoveYHash, local.z);
            _animator.SetFloat(MoveAmountHash, new Vector2(local.x, local.z).magnitude);
        }

        /// <summary>
        /// 사격 중에는 상체 레이어만 켜서 하체 이동은 그대로 둔다.
        /// </summary>
        private void ApplyShootLayer()
        {
            if (_animator == null)
                return;

            if (_topLayerIndex < 0)
            {
                _topLayerIndex = _animator.GetLayerIndex(TopLayerName);
                if (_topLayerIndex < 0)
                    return;
            }

            var shooting = _isFireHeld && _stateMachine.CurrentId != CharacterStateId.Die;
            _animator.SetLayerWeight(_topLayerIndex, shooting ? 1f : 0f);
        }

        private void PlayWalkStep()
        {
            if (_stateMachine.CurrentId != CharacterStateId.Move)
            {
                _walkStepTimer = 0f;
                return;
            }

            _walkStepTimer -= Time.deltaTime;
            if (_walkStepTimer > 0f)
                return;

            var speed = _info != null ? _info.MoveSpeed : MoveSpeed;
            _walkStepTimer = speed > 0.01f ? WalkStepDistance / speed : WalkStepDistance;
            var sound = GameManager.Instance != null ? GameManager.Instance.SoundManager : null;
            sound?.Play(SoundType.Se, WalkSeAddress);
        }

        /// <summary>
        /// 이동 방향만 받는다. 길이는 속력에 쓰이지 않는다.
        /// 입력이 없으면 Idle, 있으면 Move. Die 중에는 무시한다.
        /// </summary>
        public void SetMoveDirection(Vector3 direction)
        {
            if (_stateMachine.CurrentId == CharacterStateId.Die)
                return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                _moveDirection = Vector3.zero;
                ChangeState(CharacterStateId.Idle);
                return;
            }

            _moveDirection = direction.normalized;
            ChangeState(CharacterStateId.Move);
        }

        public void ApplyMoveVelocity()
        {
            if (_rigidbody == null)
                return;

            if (_hasMoveInput)
                _moveDirection = ToCameraPlanarDirection(_moveInput);

            var speed = _info != null ? _info.MoveSpeed : MoveSpeed;
            var delta = _moveDirection * (speed * Time.fixedDeltaTime);
            delta.y = 0f;
            if (delta.sqrMagnitude <= 0.000001f)
                return;

            _rigidbody.MovePosition(ResolveMove(delta));
        }

        /// <summary>
        /// 키네마틱은 벽에 스스로 멈추지 않는다. 바닥은 지나치고, 벽과 다른 몸에는 막힌 뒤 면을 따라간다.
        /// </summary>
        private Vector3 ResolveMove(Vector3 delta)
        {
            const float skin = 0.03f;
            var position = _rigidbody.position;
            var distance = delta.magnitude;
            var direction = delta / distance;
            if (!TryGetBlock(direction, distance, out var hit))
                return position + delta;

            var travel = Mathf.Max(0f, hit.distance - skin);
            position += direction * travel;

            var remain = distance - travel;
            if (remain <= 0.001f)
                return position;

            var slide = Vector3.ProjectOnPlane(delta, hit.normal);
            slide.y = 0f;
            if (slide.sqrMagnitude <= 0.000001f)
                return position;

            var slideDirection = slide.normalized;
            if (!TryGetBlock(slideDirection, remain, out var slideHit))
                return position + slideDirection * remain;

            var slideTravel = Mathf.Max(0f, slideHit.distance - skin);
            return position + slideDirection * slideTravel;
        }

        private bool TryGetBlock(Vector3 direction, float distance, out RaycastHit closest)
        {
            closest = default;
            var hits = _rigidbody.SweepTestAll(direction, distance, QueryTriggerInteraction.Ignore);
            var found = false;
            var best = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].normal.y > 0.7f)
                    continue;

                if (hits[i].distance >= best)
                    continue;

                best = hits[i].distance;
                closest = hits[i];
                found = true;
            }

            return found;
        }

        public void StopMoveVelocity()
        {
            _moveDirection = Vector3.zero;
            if (_rigidbody == null)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// 수평 방향을 바라보게 돌린다. 길이와 높이는 쓰지 않고, RotateSpeed만큼만 회전한다.
        /// </summary>
        public void CharacterRotate(Vector3 direction)
        {
            if (_stateMachine.CurrentId == CharacterStateId.Die)
                return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var current = _rigidbody != null ? _rigidbody.rotation : transform.rotation;
            var rotation = Quaternion.RotateTowards(current, target, RotateSpeed * Time.deltaTime);
            if (_rigidbody != null)
                _rigidbody.rotation = rotation;
            else
                transform.rotation = rotation;
        }

        /// <summary>
        /// 월드 지점을 바라본다. 방향은 CharacterRotate로 넘긴다.
        /// </summary>
        public void CharacterRotateToLook(Vector3 worldPosition)
        {
            CharacterRotate(worldPosition - transform.position);
        }

        private void RotatePlayerTowardPointer()
        {
            if (_input == null || _stateMachine.CurrentId == CharacterStateId.Die)
                return;

            if (!TryGetPointerOnGround(_input.PointerPosition, out var point))
                return;

            CharacterRotateToLook(point);
        }

        private bool TryGetPointerOnGround(Vector2 screenPosition, out Vector3 point)
        {
            point = default;
            var camera = Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(screenPosition);
            var ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!ground.Raycast(ray, out var distance))
                return false;

            point = ray.GetPoint(distance);
            return true;
        }

        public void ChangeState(CharacterStateId id)
        {
            _stateMachine.Change(id, this);
        }
    }
}
