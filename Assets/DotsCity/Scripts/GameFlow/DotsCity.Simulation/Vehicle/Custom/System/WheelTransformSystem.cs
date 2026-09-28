using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateInGroup(typeof(BeforeTransformGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct WheelTransformSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<WheelInput, WheelHandlingTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var transformJob = new TransformJob() { };

            transformJob.ScheduleParallel();
        }

        [WithAll(typeof(WheelHandlingTag))]
        [BurstCompile]
        public partial struct TransformJob : IJobEntity
        {
            void Execute(
                ref LocalTransform transform,
                in WheelInput input,
                in WheelContact contact,
                in WheelOutput output,
                in WheelOrigin wheelOrigin)
            {
                var originTransform = input.LocalTransform;
                transform.Position = originTransform.pos - math.rotate(originTransform.rot, math.up()) * (contact.CurrentSuspensionLength) - wheelOrigin.Offset;
                transform.Rotation = math.mul(originTransform.rot, quaternion.AxisAngle(math.right() * wheelOrigin.InversionValue, output.Rotation));
            }
        }
    }
}