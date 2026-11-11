namespace SHIN
{
    public class CharacterMoveState : CharacterState
    {
        protected override void OnFixedUpdate()
        {
            Character.ApplyMoveVelocity();
        }

        protected override void OnExit()
        {
            Character.StopMoveVelocity();
        }
    }
}
