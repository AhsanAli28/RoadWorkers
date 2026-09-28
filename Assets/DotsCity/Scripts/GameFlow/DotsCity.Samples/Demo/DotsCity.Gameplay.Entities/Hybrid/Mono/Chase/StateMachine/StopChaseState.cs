using Spirit604.StateMachine;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class StopChaseState : StateBase
    {
        [SerializeField] private ChasingCarBehaviour car;

        public override void EnterState()
        {
            base.EnterState();
            car.GasInput = 0;
        }
    }
}
