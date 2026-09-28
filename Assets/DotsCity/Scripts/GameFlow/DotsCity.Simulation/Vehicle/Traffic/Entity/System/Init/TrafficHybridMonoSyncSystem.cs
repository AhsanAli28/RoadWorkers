using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MonoSyncGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficHybridMonoSyncSystem : SystemBase
    {
        private const int InterpolateSpeed = 10;

        protected override void OnUpdate()
        {
            var dt = SystemAPI.Time.DeltaTime;

            var vehicleLightLookup = SystemAPI.GetComponentLookup<VehicleLightComponent>(true);

            Entities
            .WithoutBurst()
            .WithReadOnly(vehicleLightLookup)
            .ForEach((
                Entity entity,
                CarEntityAdapter adapter,
                ref LocalTransform localTransform,
                ref SpeedComponent speedComponent,
                ref VelocityComponent velocityComponent,
                ref MonoAdapterComponent monoAdapterComponent,
                in VehicleInputReader vehicleInputReader,
                in CullStateComponent cullStateComponent) =>
            {
                if (adapter.CurrentPhysicsState == CarEntityAdapter.PhysicsState.FullEnabled)
                {
                    velocityComponent.Value = adapter.Velocity;
                    speedComponent.Value = adapter.Velocity.magnitude;

                    localTransform.Position = adapter.Transform.position;
                    localTransform.Rotation = adapter.Transform.rotation;

                    adapter.UpdateInput(in vehicleInputReader);
                }
                else if (adapter.CurrentPhysicsState == CarEntityAdapter.PhysicsState.Disabled)
                {
                    bool synced = monoAdapterComponent.Synced;

                    if (synced && monoAdapterComponent.Interpolate)
                    {
                        monoAdapterComponent.Position = math.lerp(monoAdapterComponent.Position, localTransform.Position, dt * InterpolateSpeed);
                        monoAdapterComponent.Rotation = math.slerp(monoAdapterComponent.Rotation, localTransform.Rotation, dt * InterpolateSpeed);
                    }
                    else
                    {
                        if (!synced)
                            monoAdapterComponent.Synced = true;

                        monoAdapterComponent.Position = localTransform.Position;
                        monoAdapterComponent.Rotation = localTransform.Rotation;
                    }

                    adapter.Transform.position = monoAdapterComponent.Position;
                    adapter.Transform.rotation = monoAdapterComponent.Rotation;
                }

                if (adapter.CheckCullState(cullStateComponent.State))
                {
                    monoAdapterComponent.Synced = false;
                }

                if (adapter.CarLight != null)
                {
#if UNITY_EDITOR
                    var vehicleLight = vehicleLightLookup[entity];

                    if (vehicleLightLookup.HasComponent(entity))
                    {
                        adapter.CarLight.UpdateState(in vehicleLight);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Vehicle light component not found. Make sure that you enabled it in the <b>GeneralSettings</b>");
                    }
#else
                    var vehicleLight = vehicleLightLookup[entity];
                    adapter.CarLight.UpdateState(in vehicleLight);
#endif
                }

            }).Run();
        }
    }
}