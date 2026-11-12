using UnityEngine;

namespace SHIN
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterBase : MonoBehaviour
    {
        private const float MoveSpeed = 5f;
        private const float RotateSpeed = 1080f;

        private readonly CharacterStateMachine _stateMachine = new CharacterStateMachine();

        private Rigidbody _rigidbody;
        private CharacterType _characterType = CharacterType.None;
        private Vector3 _moveDirection;
        private Vector2 _moveInput;
        private bool _hasMoveInput;
        private InputManager _input;

        private void Reset()
        {
            InitRigidbody();
        }

        private void InitRigidbody()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
                return;

            body.isKinematic = false;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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

            if (_rigidbody.isKinematic)
                Debug.LogWarning("[CharacterBase] Kinematic Rigidbody는 속도로 이동하지 않습니다. Is Kinematic을 끄세요.", this);

            if (GetComponentInChildren<Collider>() == null)
                Debug.LogWarning("[CharacterBase] Collider가 없습니다.", this);
        }

        public CharacterType CharacterType => _characterType;

        public void InitCharacter(CharacterType characterType)
        {
            _characterType = characterType;
            InitRigidbody();
            ChangeState(CharacterStateId.Idle);
        }

        private void Update()
        {
            _stateMachine.Update();
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
            OnMoveInput(_input.Move);
        }

        public void UnbindInput()
        {
            if (_input == null)
                return;

            _input.MoveChanged -= OnMoveInput;
            _input = null;
            _hasMoveInput = false;
            _moveInput = Vector2.zero;
        }

        private void OnDestroy()
        {
            UnbindInput();
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
            if (move.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            var planar = new Vector3(move.x, 0f, move.y);
            var camera = Camera.main;
            if (camera == null)
                return planar.normalized;

            var yaw = Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f);
            var direction = yaw * planar;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return direction.normalized;
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
            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            if (_hasMoveInput)
                _moveDirection = ToCameraPlanarDirection(_moveInput);

            _rigidbody.linearVelocity = _moveDirection * MoveSpeed;
        }

        public void StopMoveVelocity()
        {
            _moveDirection = Vector3.zero;
            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
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
