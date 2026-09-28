using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.TestScene
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class VehicleLineTracker : BeginSimulationSystemBase
    {
        private EntityQuery vehicleQuery;
        private NativeArray<bool> flag = new NativeArray<bool>(1, Allocator.Persistent);
        private VehicleCustomStressUI customUI;

        protected override void OnCreate()
        {
            base.OnCreate();

            vehicleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VehicleInputReader>()
                .Build(this);

            RequireForUpdate<FirstRowVehicleTag>();
            RequireForUpdate<FinishPointTag>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (flag.IsCreated)
            {
                flag.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            var finishPointEntity = SystemAPI.GetSingletonEntity<FinishPointTag>();
            var pos = EntityManager.GetComponentData<LocalTransform>(finishPointEntity).Position;

            var entities = vehicleQuery.ToEntityArray(Allocator.TempJob);

            var commandBuffer = GetCommandBuffer();
            var flagLocal = flag;

            flagLocal[0] = false;

            Entities
            .WithBurst()
            .WithAll<FirstRowVehicleTag>()
            .ForEach((Entity e, in LocalTransform transform) =>
            {
                var distance = math.distance(transform.Position, pos);

                if (distance < 1 || transform.Position.y < -1)
                {
                    flagLocal[0] = true;

                    PoolEntityUtils.DestroyEntity(ref commandBuffer, entities);
                }

            }).Schedule();

            Dependency.Complete();

            entities.Dispose();

            if (flagLocal[0])
            {
                customUI.TemporalilyDisable(() => vehicleQuery.CalculateEntityCount() == 0);
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<VehicleStressSpawner>().Enabled = true;
            }

            AddCommandBufferForProducer();
        }

        public void Initialize(VehicleCustomStressUI customUI)
        {
            this.customUI = customUI;
        }
    }
}