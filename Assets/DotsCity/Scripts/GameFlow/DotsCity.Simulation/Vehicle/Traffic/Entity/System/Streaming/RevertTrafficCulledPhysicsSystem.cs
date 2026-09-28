using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RevertTrafficCulledPhysicsSystem : ISystem
    {
        private const float CastDistance = 2f;
        private const float CastOffset = 0.5f;

        private EntityQuery revertQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            revertQuery = SystemAPI.QueryBuilder()
                .WithNone<InPermittedRangeTag, CulledEventTag>()
                .WithAll<PhysicsWorldIndex, CullPhysicsTag, InViewOfCameraTag, TrafficTag, CustomCullPhysicsTag, TrafficPathComponent>()
                .WithAllRW<LocalTransform, PhysicsVelocity>()
                .Build();

            revertQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = ProjectConstants.NoPhysicsWorldIndex });

            state.RequireForUpdate(revertQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var revertPhysicsJob = new RevertPhysicsJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                VelocityLookup = SystemAPI.GetComponentLookup<VelocityComponent>(true),
                PhysicsGraphicalInterpolationBufferLookup = SystemAPI.GetComponentLookup<PhysicsGraphicalInterpolationBuffer>(true),
                VehicleWheelLookup = SystemAPI.GetBufferLookup<VehicleWheel>(true),
                VehicleOutputLookup = SystemAPI.GetComponentLookup<VehicleOutput>(false),
                WheelContactLookup = SystemAPI.GetComponentLookup<WheelContact>(false),
                WheelContactVelocityLookup = SystemAPI.GetComponentLookup<WheelContactVelocity>(false),
                WheelOutputLookup = SystemAPI.GetComponentLookup<WheelOutput>(false),
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            revertPhysicsJob.Run(revertQuery);
        }

        [WithNone(typeof(InPermittedRangeTag), typeof(CulledEventTag))]
        [WithAll(typeof(PhysicsWorldIndex), typeof(InViewOfCameraTag), typeof(CullPhysicsTag), typeof(TrafficTag), typeof(CustomCullPhysicsTag))]
        [BurstCompile]
        public partial struct RevertPhysicsJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<VelocityComponent> VelocityLookup;

            [ReadOnly]
            public ComponentLookup<PhysicsGraphicalInterpolationBuffer> PhysicsGraphicalInterpolationBufferLookup;

            [ReadOnly]
            public BufferLookup<VehicleWheel> VehicleWheelLookup;

            public ComponentLookup<VehicleOutput> VehicleOutputLookup;
            public ComponentLookup<WheelContact> WheelContactLookup;
            public ComponentLookup<WheelContactVelocity> WheelContactVelocityLookup;
            public ComponentLookup<WheelOutput> WheelOutputLookup;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref PhysicsVelocity physicsVelocity,
                in TrafficPathComponent trafficPathComponent)
            {
                if (VelocityLookup.HasComponent(entity))
                {
                    physicsVelocity.Linear = VelocityLookup[entity].Value;
                }

                var pathLine = math.normalize(trafficPathComponent.DestinationWayPoint - trafficPathComponent.PreviousDestination);

                var point = VectorExtensions.FindNearestPointOnLine(trafficPathComponent.PreviousDestination, pathLine, transform.Position);

                var diff = point.y - transform.Position.y;

                if (diff > 0 && diff < 1f)
                {
                    var right = transform.Right();
                    var trafficForwardX = Vector3.ProjectOnPlane(transform.Forward(), right);
                    var pathLineX = Vector3.ProjectOnPlane(pathLine, right);

                    var signedAngle = Vector3.SignedAngle(trafficForwardX, pathLineX, Vector3.forward);

                    if (math.abs(signedAngle) < 40)
                    {
                        var fixRotation = quaternion.RotateX(math.radians(signedAngle));
                        transform.Rotation = math.mul(transform.Rotation, fixRotation);
                    }

                    transform.Position.y = point.y;
                }

                CommandBuffer.SetSharedComponent(entity, new PhysicsWorldIndex()
                {
                    Value = 0
                });

                if (VehicleWheelLookup.HasBuffer(entity))
                {
                    var buffer = VehicleWheelLookup[entity];

                    VehicleOutputLookup[entity] = new VehicleOutput()
                    {
                        BlockTime = Time + 0.3f
                    };

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        WheelContactLookup[buffer[i].WheelEntity] = new WheelContact();
                        WheelContactVelocityLookup[buffer[i].WheelEntity] = new WheelContactVelocity();
                        WheelOutputLookup[buffer[i].WheelEntity] = new WheelOutput();
                    }
                }

                if (PhysicsGraphicalInterpolationBufferLookup.HasComponent(entity))
                {
                    CommandBuffer.AddComponent(entity, new PhysicsGraphicalSmoothing()
                    {
                        CurrentVelocity = physicsVelocity
                    });
                }
            }
        }
    }
}