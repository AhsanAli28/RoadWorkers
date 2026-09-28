using Spirit604.StateMachine;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class ChaseTransition : TransitionCondition
    {
        [SerializeField] private ChasingCarBehaviour car;

        public override void Tick()
        {
            base.Tick();

            bool shouldChase = car.DistanceToTarget > car.MinDistanceToChase;

            StateResult = shouldChase ? StateResult.Success : StateResult.Fail;
        }
    }
}