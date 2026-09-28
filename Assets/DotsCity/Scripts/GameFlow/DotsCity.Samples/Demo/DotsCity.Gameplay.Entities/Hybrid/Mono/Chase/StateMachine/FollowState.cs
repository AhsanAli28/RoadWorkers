using Spirit604.Extensions;
using Spirit604.StateMachine;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.DotsCity.Gameplay.Chaser.StateMachine
{
    public class FollowState : StateBase
    {
        [SerializeField] private ChasingCarBehaviour car;

        private float nextAttemptTime;
        private bool isSideChasing;

        public override void EnterState()
        {
            base.EnterState();

            isSideChasing = false;
        }

        public override void Tick()
        {
            base.Tick();

            car.GasInput = 1;

            Vector3 targetPoint = Vector3.zero;

            if (!isSideChasing && UnityEngine.Time.time > nextAttemptTime)
            {
                isSideChasing = true;
            }

            Vector3 myPosition = transform.position.Flat();
            Vector3 targetPosition = car.TargetPosition.Flat();

            if (!car.CanSideChasing || !isSideChasing)
            {
                targetPoint = targetPosition + (myPosition - targetPosition).normalized * car.TargetOffset;
            }
            else
            {
                int side = car.ChaseIndex == 0 ? -1 : 1;
                targetPoint = targetPosition - car.TargetForward * car.SideChaseDistance;
                targetPoint = targetPoint + Vector3.Cross(Vector3.up * side, car.TargetForward) * car.TargetOffset;
            }

            car.NavMeshAgent.SetDestination(targetPoint);

            car.TargetWayPoint = car.NavMeshAgent.steeringTarget.Flat();

            if (isSideChasing && car.NavMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                isSideChasing = false;
                nextAttemptTime = UnityEngine.Time.time + car.CalculateFollowAttemptTime;
            }

            Vector3 direction = (car.TargetWayPoint - myPosition).normalized;

            float angle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
            car.DesiredSteeringAngle = angle;
            car.SetSpeedLimit(car.SlowingMultiplier);
        }
    }
}
