using Spirit604.StateMachine;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class StartFollowTransition : TransitionCondition
    {
        [SerializeField] private ChasingCarBehaviour car;

        public override void Tick()
        {
            base.Tick();

            bool shouldFollow = car.DistanceToTarget < car.MaxDistanceToFollow;

            StateResult = shouldFollow ? StateResult.Success : StateResult.Fail;
        }
    }
}