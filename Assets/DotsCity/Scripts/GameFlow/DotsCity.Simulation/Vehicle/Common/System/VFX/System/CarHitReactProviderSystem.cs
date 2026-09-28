using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spirit604.DotsCity.Simulation.Car
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class CarHitReactProviderSystem : SystemBase
    {
        public struct FactoryCreatedEventTag : IComponentData { }

        private const string HitShaderTextureName = "_MainTex";
        private RenderMeshArray rendererMeshArray;

        public static NativeArray<BatchMaterialID> BatchMaterials { get; private set; }
        public static NativeArray<BatchMeshID> BatchMeshes { get; private set; }
        public static NativeHashSet<int> TakenIndexes { get; private set; }
        public static Entity PrefabEntity { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (BatchMaterials.IsCreated)
            {
                BatchMaterials.Dispose();
                BatchMaterials = default;
            }

            if (BatchMeshes.IsCreated)
            {
                BatchMeshes.Dispose();
                BatchMeshes = default;
            }

            if (TakenIndexes.IsCreated)
            {
                TakenIndexes.Dispose();
                TakenIndexes = default;
            }

            PrefabEntity = default;
        }

        protected override void OnUpdate() { }

        public EntityQuery GetPrefabQuery() => new EntityQueryBuilder(Allocator.Temp)
            .WithAll<HitReactionVehicleBodyPrefab>()
            .WithOptions(EntityQueryOptions.IncludePrefab)
            .Build(EntityManager);

        public void Initialize()
        {
            var settings = EntityManager.CreateEntityQuery(typeof(TrafficGeneralSettingsReference)).GetSingleton<TrafficGeneralSettingsReference>();

            if (!settings.Config.Value.CarVisualDamageSystemSupport)
            {
                return;
            }

            var entitiesGraphicsSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();

            var configQuery = EntityManager.CreateEntityQuery(typeof(CarHitReactionConfigReference));
            var configEntity = configQuery.GetSingletonEntity();

            var carHitReactionConfigReference = configQuery.GetSingleton<CarHitReactionConfigReference>();

            var hitReactionMaterialData = EntityManager.GetSharedComponentManaged<HitReactionMaterialData>(configEntity);

            var hitReactionMaterial = hitReactionMaterialData.Material;

            if (!hitReactionMaterial)
            {
                UnityEngine.Debug.LogError("CarHitReactProviderSystem. HitReaction material is null");
                return;
            }

            var poolSize = carHitReactionConfigReference.Config.Value.PoolSize;

            var trafficSettings = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();

            var trafficPrefabQuery = EntityManager.CreateEntityQuery(typeof(TrafficPrefabData), typeof(TrafficPrefabSort));

            trafficPrefabQuery.SetSharedComponentFilter(new TrafficPrefabSort()
            {
                TrafficEntityType = trafficSettings.Reference.Value.EntityType
            });

            var vehicleCount = trafficPrefabQuery.CalculateEntityCount();
            var globalPoolSize = poolSize * vehicleCount;

            var newMaterials = new Material[globalPoolSize];
            var newMeshes = new Mesh[globalPoolSize];

            var batchMaterialIDs = new NativeArray<BatchMaterialID>(globalPoolSize, Allocator.Persistent);
            var batchMeshIDs = new NativeArray<BatchMeshID>(globalPoolSize, Allocator.Persistent);

            var trafficPrefabs = trafficPrefabQuery.ToComponentDataArray<TrafficPrefabData>(Allocator.TempJob);

            for (int i = 0; i < trafficPrefabs.Length; i++)
            {
                var prefabEntity = trafficPrefabs[i].PrefabEntity;
                var hullEntity = EntityManager.GetComponentData<CarRelatedHullComponent>(prefabEntity).HullEntity;
                var carModel = EntityManager.GetComponentData<CarModelComponent>(prefabEntity).LocalIndex;
                var startIndex = carModel * poolSize;

                if (startIndex == -1)
                {
                    UnityEngine.Debug.Log($"CarHitReactProviderSystem. HitReaction '{carModel}' binding not found");
                    continue;
                }

                if (!EntityManager.HasComponent<RenderMeshArray>(hullEntity))
                {
                    if (EntityManager.HasComponent<WorldRenderBounds>(hullEntity))
                    {
                        UnityEngine.Debug.Log($"CarHitReactProviderSystem. HitReaction '{carModel}' RenderMeshArray component not found. Plz reimport subscene.");
                    }

                    continue;
                }

                var materialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(hullEntity);
                var renderArray = EntityManager.GetSharedComponentManaged<RenderMeshArray>(hullEntity);

                var mesh = renderArray.GetMesh(materialMeshInfo);
                var material = renderArray.GetMaterial(materialMeshInfo);

                for (int j = 0; j < poolSize; j++)
                {
                    var newMaterial = GameObject.Instantiate(hitReactionMaterial);
                    var newMesh = GameObject.Instantiate(mesh);
                    newMesh.MarkDynamic();

                    newMaterial.SetTexture(HitShaderTextureName, material.mainTexture);

                    var currentIndex = startIndex + j;

                    newMaterials[currentIndex] = newMaterial;
                    newMeshes[currentIndex] = newMesh;

                    var materialBatchId = entitiesGraphicsSystem.RegisterMaterial(newMaterial);
                    var meshBatchId = entitiesGraphicsSystem.RegisterMesh(newMesh);

                    batchMaterialIDs[currentIndex] = materialBatchId;
                    batchMeshIDs[currentIndex] = meshBatchId;
                }
            }

            trafficPrefabs.Dispose();

            rendererMeshArray = new RenderMeshArray(newMaterials, newMeshes);

            var takenIndexes = new NativeHashSet<int>(batchMeshIDs.Length, Allocator.Persistent);

            BatchMaterials = batchMaterialIDs;
            BatchMeshes = batchMeshIDs;
            TakenIndexes = takenIndexes;

            var hitReactionPrefab = GetPrefabQuery().GetSingleton<HitReactionVehicleBodyPrefab>().PrefabEntity;

            EntityManager.SetSharedComponentManaged(hitReactionPrefab, rendererMeshArray);

            PrefabEntity = hitReactionPrefab;

            EntityManager.CreateEntity(typeof(CarHitReactProviderSystem.FactoryCreatedEventTag));
        }
    }
}