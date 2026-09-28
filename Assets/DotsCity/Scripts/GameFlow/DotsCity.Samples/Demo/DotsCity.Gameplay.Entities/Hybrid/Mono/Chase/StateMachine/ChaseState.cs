using Spirit604.Extensions;
using Spirit604.StateMachine;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class ChaseState : StateBase
    {
        [SerializeField] private ChasingCarBehaviour car;

        //private bool TargetIsSlow => car.DistanceToTarget < car.MaxDistanceForSlowing && (car.Target.Speed < car.Target.MaxSpeed * 0.6f || car.Target.MaxSpeed < car.SlowTargetMaxSpeed);
        private bool TargetIsSlow => false;

        public override void LeaveState()
        {
            base.LeaveState();

            car.ForceDirection = default;
        }

        public override void Tick()
        {
            base.Tick();

            Vector3 targetPosition = car.TargetPosition.Flat();
            Vector3 myPosition = transform.position.Flat();

            Vector3 forceDirection = Vector3.zero;

            Vector3 targetPoint = targetPosition + (myPosition - targetPosition).normalized * car.TargetOffset;

            if (!car.NavMeshAgent.isOnNavMesh)
                return;

            car.NavMeshAgent.SetDestination(targetPoint);

            car.TargetWayPoint = car.NavMeshAgent.steeringTarget.Flat();

            Vector3 direction = (car.TargetWayPoint - myPosition).normalized;

            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

            car.GasInput = 1;
            car.DesiredSteeringAngle = angle;

            Vector3 checkPosition = myPosition + (targetPosition - myPosition).normalized * 2;
            bool inViewOfCamera = car.MainCamera.InViewOfCamera(checkPosition);

            if (!inViewOfCamera)
            {
                forceDirection = direction;
            }

            car.ForceDirection = forceDirection;

            if (TargetIsSlow)
            {
                car.SetSpeedLimit(car.SlowingMultiplier);
            }
            else
            {
                float speedmultiplier = 1f;

                if (!inViewOfCamera)
                {
                    speedmultiplier = car.AccelerationSpeedMultiplier;
                }

                car.SetSpeedLimit(speedmultiplier);
            }
        }
    }
}
