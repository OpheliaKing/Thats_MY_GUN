using System.Collections.Generic;

namespace SHIN
{
    public class CharacterStateMachine
    {
        private readonly Dictionary<CharacterStateId, CharacterState> _states = new()
        {
            { CharacterStateId.Idle, new CharacterIdleState() },
            { CharacterStateId.Move, new CharacterMoveState() },
            { CharacterStateId.Die, new CharacterDieState() }
        };

        private CharacterState _current;

        public CharacterStateId? CurrentId { get; private set; }

        public void Change(CharacterStateId id, CharacterBase character)
        {
            if (CurrentId == id)
                return;

            if (!_states.TryGetValue(id, out var next))
                return;

            _current?.Exit();
            _current = next;
            CurrentId = id;
            _current.Enter(character);
        }

        public void Update()
        {
            _current?.Update();
        }

        public void FixedUpdate()
        {
            _current?.FixedUpdate();
        }
    }
}
