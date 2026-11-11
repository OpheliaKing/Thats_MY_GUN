using UnityEngine;

namespace SHIN
{
    public class CharacterBase : MonoBehaviour
    {
        private const float MoveSpeed = 5f;

        private readonly CharacterStateMachine _stateMachine = new CharacterStateMachine();

        private Rigidbody _rigidbody;
        private Vector3 _moveDirection;

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

        private void InitCharacter()
        {
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

            _rigidbody.linearVelocity = _moveDirection * MoveSpeed;
        }

        public void StopMoveVelocity()
        {
            _moveDirection = Vector3.zero;
            if (_rigidbody == null || _rigidbody.isKinematic)
                return;

            _rigidbody.linearVelocity = Vector3.zero;
        }

        public void ChangeState(CharacterStateId id)
        {
            _stateMachine.Change(id, this);
        }
    }
}
