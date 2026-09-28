using Spirit604.StateMachine;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class StopCarTransition : TransitionCondition
    {
        [SerializeField] private ChasingCarBehaviour car;

        public override void Tick()
        {
            base.Tick();

            bool closeTarget = car.DistanceToTarget < car.MinDistanceToChase;
            bool shouldStop = car.TargetVelocityValue < car.MinimumTargetVelocity;

            StateResult = shouldStop && closeTarget ? StateResult.Success : StateResult.Fail;
        }
    }
}