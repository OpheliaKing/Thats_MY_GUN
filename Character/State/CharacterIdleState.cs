namespace SHIN
{
    public class CharacterIdleState : CharacterState
    {
        protected override void OnEnter()
        {
            Character.StopMoveVelocity();
        }
    }
}
