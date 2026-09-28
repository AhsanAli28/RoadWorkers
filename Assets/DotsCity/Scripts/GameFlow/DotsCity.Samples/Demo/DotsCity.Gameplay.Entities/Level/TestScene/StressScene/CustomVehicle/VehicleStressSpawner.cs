using Spirit604.DotsCity.Simulation.Car;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.TestScene
{
    [UpdateInGroup(typeof(StructuralSystemGroup), OrderFirst = true)]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class VehicleStressSpawner : SystemBase
    {
        private VehicleCustomStressUI customUI;
        private bool spawned;
        private EntityQuery vehicleQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            vehicleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<VehicleInputReader>()
                .Build(this);

            RequireForUpdate<SpawnPointSettings>();
            RequireForUpdate<PrefabContainer>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!spawned)
            {
                customUI.EnableWithDelay(() => vehicleQuery.CalculateEntityCount() == 0);
            }
        }

        protected override void OnUpdate()
        {
            var prefabContainerQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabContainer>());

            var prefabContainers = prefabContainerQuery.ToComponentDataArray<PrefabContainer>(Allocator.TempJob);

            var spawnPointSettings = SystemAPI.GetSingleton<SpawnPointSettings>();
            var spawnPointEntity = SystemAPI.GetSingletonEntity<SpawnPointTag>();

            var prefabEntity = prefabContainers[0].Entity;

            var startPos = EntityManager.GetComponentData<LocalTransform>(spawnPointEntity);

            for (int x = 0; x < spawnPointSettings.CountPerRow; x++)
            {
                for (int z = 0; z < spawnPointSettings.Rows; z++)
                {
                    var entity = EntityManager.Instantiate(prefabEntity);

                    if (x == 0 && z == 0)
                    {
                        EntityManager.AddComponent<LastRowVehicleTag>(entity);
                    }

                    if (x == 0 && z == spawnPointSettings.Rows - 1)
                    {
                        EntityManager.AddComponent<FirstRowVehicleTag>(entity);
                    }

                    var pos = startPos.Position + new float3(x * spawnPointSettings.XOffset, 0, z * spawnPointSettings.ZOffset);

                    EntityManager.SetComponentData<LocalTransform>(entity, LocalTransform.FromPosition(pos));
                    EntityManager.SetComponentData(entity, new VehicleInputReader()
                    {
                        Throttle = 1
                    });

                    EntityManager.SetComponentData(entity, SpeedComponent.SetSpeedLimitByKmh(30f));
                }
            }

            prefabContainers.Dispose();

            Enabled = false;
            spawned = true;
        }

        public void Initialize(VehicleCustomStressUI customUI)
        {
            this.customUI = customUI;
        }
    }
}
