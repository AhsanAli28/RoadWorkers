using Spirit604.DotsCity.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class VehicleCustomBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VehicleBakingData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                in VehicleBakingData vehicleBakingData) =>
            {
                var allWheels = vehicleBakingData.AllWheels;

                for (int i = 0; i < allWheels.Length; i++)
                {
                    var wheelEntity = allWheels[i].Entity;

                    commandBuffer.AddComponent(wheelEntity, new ComponentTypeSet(new ComponentType[] {
                        typeof(WheelInput),
                        typeof(WheelContact),
                        typeof(WheelContactVelocity),
                        typeof(WheelOutput),
                        }));

                    var maxSteeringAngle = vehicleBakingData.SteeringWheels.Contains(wheelEntity) ? math.radians(vehicleBakingData.MaxSteeringAngle) : 0;

                    float powerSteering = 1f;

                    if (maxSteeringAngle > 0)
                    {
                        powerSteering = vehicleBakingData.PowerSteering;
                    }

                    commandBuffer.AddComponent(wheelEntity, new Wheel
                    {
                        VehicleEntity = vehicleBakingData.VehicleEntity,
                        Radius = vehicleBakingData.Radius,
                        Width = vehicleBakingData.Width,
                        ApplyImpulseOffset = vehicleBakingData.ApplyImpulseOffset,
                        SuspensionLength = vehicleBakingData.SuspensionLength,
                        Inertia = vehicleBakingData.WheelMass * vehicleBakingData.Radius * vehicleBakingData.Radius * 0.5f,
                        Collider = vehicleBakingData.WheelCollider,
                        ForwardFrictionValue = vehicleBakingData.ForwardFrictionValue,
                        LateralFrictionValue = vehicleBakingData.LateralFrictionValue,
                        BrakeFrictionValue = vehicleBakingData.BrakeFrictionValue,
                        CastType = vehicleBakingData.CastType,
                        CastFilter = vehicleBakingData.CastFilter,
                        PowerSteering = powerSteering
                    });

                    commandBuffer.AddComponent(wheelEntity, new WheelOrigin
                    {
                        Value = allWheels[i].Origin,
                        Offset = allWheels[i].WheelOffset,
                        InversionValue = allWheels[i].InversionValue,
                    });

                    commandBuffer.AddComponent(wheelEntity, new WheelSuspension
                    {
                        Damping = vehicleBakingData.Damping,
                        Stiffness = vehicleBakingData.Stiffness
                    });

                    commandBuffer.AddComponent(wheelEntity, new WheelFriction
                    {
                        Longitudinal = vehicleBakingData.Longitudinal,
                        Lateral = vehicleBakingData.Lateral
                    });


                    commandBuffer.AddComponent(wheelEntity, new WheelControllable
                    {
                        MaxSteeringAngle = maxSteeringAngle,
                        DriveRate = allWheels[i].DriveRate,
                        BrakeRate = allWheels[i].BrakeRate,
                        HandbrakeRate = allWheels[i].HandbrakeRate
                    });

                    commandBuffer.AddComponent(wheelEntity, new WheelBrakes
                    {
                        BrakeTorque = vehicleBakingData.BrakeTorque,
                        HandbrakeTorque = vehicleBakingData.HandbrakeTorque
                    });

                    commandBuffer.AddComponent<WheelHandlingTag>(wheelEntity);

#if UNITY_EDITOR
                    commandBuffer.AddSharedComponent(wheelEntity, new WheelDebugShared
                    {
                        ShowDebug = vehicleBakingData.ShowDebug
                    });
#endif
                }

                if (!EntityManager.HasComponent<SpeedComponent>(vehicleBakingData.VehicleEntity))
                {
                    commandBuffer.AddComponent(vehicleBakingData.VehicleEntity, new SpeedComponent()
                    {
                        CurrentLimit = -1
                    });
                }

                if (!EntityManager.HasComponent<VelocityComponent>(vehicleBakingData.VehicleEntity))
                {
                    commandBuffer.AddComponent<VelocityComponent>(vehicleBakingData.VehicleEntity);
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}