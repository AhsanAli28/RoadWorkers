using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(MonoSyncGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerCarMonoSyncVelocitySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<PlayerTag>()
            .WithBurst()
            .ForEach((
                Entity entity,
                Rigidbody rb,
                ref VelocityComponent velocityComponent,
                ref SpeedComponent speedComponent) =>
            {
#if UNITY_6000_0_OR_NEWER
                var velocity = rb.linearVelocity;
#else
                var velocity = rb.velocity;
#endif

                velocityComponent.Value = velocity;
                speedComponent.Value = Mathf.Abs(Vector3.Dot(rb.transform.forward, velocity));
            }).Run();
        }
    }
}