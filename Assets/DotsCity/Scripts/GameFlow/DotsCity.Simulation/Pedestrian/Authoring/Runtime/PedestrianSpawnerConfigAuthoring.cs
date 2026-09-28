using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianSpawnerConfigAuthoring : RuntimeEntityConfigBase
    {
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private BlobAssetReference<PedestrianSpawnSettings> blobRef;
        private EntityQuery pedestrianQuery;

        [InjectWrapper]
        public void Construct(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            this.pedestrianSpawnerConfigHolder = pedestrianSpawnerConfigHolder;
        }

        private PedestrianSpawnerConfig PedestrianSpawnerConfig => pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig;

        protected override bool AutoSync => false;

        private void Awake()
        {
            pedestrianQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianMovementSettings>());
        }

        public void ChangeDensity(int count, bool destroyOnReduce = true)
        {
            if (PedestrianSpawnerConfig.MinPedestrianCount > count && destroyOnReduce)
            {
                var reduceValue = PedestrianSpawnerConfig.MinPedestrianCount - count;

                var entities = pedestrianQuery.ToEntityArray(Allocator.Temp);

                reduceValue = Mathf.Min(reduceValue, entities.Length);

                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < reduceValue; i++)
                {
                    commandBuffer.SetComponentEnabled<PooledEventTag>(entities[i], true);
                }

                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();
                entities.Dispose();
            }

            PedestrianSpawnerConfig.MinPedestrianCount = count;

            UpdateConfig();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (blobRef.IsCreated)
            {
                blobRef.Dispose();
            }
        }

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            PedestrianSpawnerConfig config = null;

            if (pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig)
            {
                config = pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig;
            }

            if (!config)
            {
                Debug.LogError("PedestrianSpawnerConfigAuthoring. PedestrianSpawnerConfigHolder is pedestrianSpawnerConfig is null");
                return;
            }

            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PedestrianSpawnSettings>();

                root.MinPedestrianCount = config.MinPedestrianCount;
                root.MaxPedestrianPerNode = config.MaxPedestrianPerNode;
                root.MinSpawnDelay = config.MinSpawnDelay;
                root.MaxSpawnDelay = config.MaxSpawnDelay;

                blobRef = builder.CreateBlobAssetReference<PedestrianSpawnSettings>(Allocator.Persistent);

                dstManager.AddComponentData(entity, new PedestrianSpawnSettingsReference() { Config = blobRef });
            }
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().UpdateSpawnConfig();
        }
    }
}
