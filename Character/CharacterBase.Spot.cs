using UnityEngine;

namespace SHIN
{
    public partial class CharacterBase
    {
        static readonly int JumpHash = Animator.StringToHash("Jump");
        static readonly int IdleHash = Animator.StringToHash("Idle");

        bool _spotJumping;
        int _spotJumpCount;

        public bool IsSpotJumping => _spotJumping;

        public void PlaySpotJump(int count)
        {
            if (_animator == null || count <= 0 || _stateMachine.CurrentId == CharacterStateId.Die)
                return;

            _spotJumpCount = count;
            _spotJumping = true;
            _animator.CrossFade(JumpHash, 0.05f, 0, 0f);
        }

        public void StopSpotJump()
        {
            _spotJumping = false;
        }

        void UpdateSpotJump()
        {
            if (!_spotJumping || _animator == null)
                return;

            if (_stateMachine.CurrentId == CharacterStateId.Die)
            {
                _spotJumping = false;
                return;
            }

            if (_animator.IsInTransition(0))
                return;

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != JumpHash || state.normalizedTime < _spotJumpCount)
                return;

            _spotJumping = false;
            _animator.CrossFade(IdleHash, 0.1f, 0, 0f);
        }
    }
}
