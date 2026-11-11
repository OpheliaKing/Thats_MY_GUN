namespace SHIN
{
    public abstract class CharacterState
    {
        protected CharacterBase Character { get; private set; }

        public void Enter(CharacterBase character)
        {
            Character = character;
            OnEnter();
        }

        public void Update()
        {
            OnUpdate();
        }

        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        public void Exit()
        {
            OnExit();
        }

        protected virtual void OnEnter()
        {
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnFixedUpdate()
        {
        }

        protected virtual void OnExit()
        {
        }
    }
}
