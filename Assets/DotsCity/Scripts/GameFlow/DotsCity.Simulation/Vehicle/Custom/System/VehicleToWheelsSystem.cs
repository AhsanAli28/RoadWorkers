using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateInGroup(typeof(PhysicsSimGroup), OrderFirst = true)]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct VehicleToWheelsSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<
                    PhysicsWorldIndex,
                    VehicleWheel,
                    LocalTransform,
                    PhysicsMass,
                    VehicleInput,
                    VehicleEngine,
                    VehicleOutput>()
                .Build();

            updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var vehicleToWheelsJob = new VehicleToWheelsJob()
            {
                WheelOriginLookup = SystemAPI.GetComponentLookup<WheelOrigin>(true),
                WheelControllableLookup = SystemAPI.GetComponentLookup<WheelControllable>(true),
                WheelInputLookup = SystemAPI.GetComponentLookup<WheelInput>(false),
            };

            vehicleToWheelsJob.Schedule(updateQuery);
        }

        [WithAll(typeof(PhysicsWorldIndex))]
        [BurstCompile]
        public partial struct VehicleToWheelsJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<WheelOrigin> WheelOriginLookup;

            [ReadOnly]
            public ComponentLookup<WheelControllable> WheelControllableLookup;

            public ComponentLookup<WheelInput> WheelInputLookup;

            void Execute(
                in DynamicBuffer<VehicleWheel> wheels,
                in LocalTransform transform,
                in PhysicsMass mass,
                in VehicleInput input,
                in VehicleEngine engine,
                in VehicleOutput output)
            {
                var engineTorque = engine.EvaluateTorque(engine.TransmitWheelRotationSpeedToEngineRpm(output.MaxWheelRotationSpeed)) * input.Throttle;

                var wheelsTorque = engine.TransmitEngineTorqueToWheelTorque(engineTorque);

                for (int i = 0; i < wheels.Length; i++)
                {
                    var wheelEntity = wheels[i].WheelEntity;

                    var origin = WheelOriginLookup[wheelEntity];
                    var controllable = WheelControllableLookup[wheelEntity];

                    var localTransform = origin.Value;
                    var steeringRotation = quaternion.AxisAngle(math.up(), input.Steering * controllable.MaxSteeringAngle);

                    localTransform.rot = math.mul(localTransform.rot, steeringRotation);

                    var worldTransform = math.mul(new RigidTransform(transform.Rotation, transform.Position), new RigidTransform(steeringRotation, localTransform.pos));

                    var wheelInput = new WheelInput
                    {
                        LocalTransform = localTransform,
                        LocalToWorld = worldTransform,
                        Up = math.rotate(worldTransform.rot, math.up()),
                        MassMultiplier = 1.0f / mass.InverseMass,
                        Torque = wheelsTorque * controllable.DriveRate,
                        Brake = controllable.BrakeRate * input.Brake,
                        Handbrake = controllable.HandbrakeRate * input.Handbrake,
                    };

                    WheelInputLookup[wheelEntity] = wheelInput;
                }
            }
        }
    }
}