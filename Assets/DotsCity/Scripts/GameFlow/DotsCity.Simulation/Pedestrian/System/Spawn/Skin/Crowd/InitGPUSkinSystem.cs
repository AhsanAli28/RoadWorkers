using Spirit604.AnimationBaker.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class InitGPUSkinSystem : SystemBase
    {
        private CrowdSkinProviderSystem crowdSkinProviderSystem;
        private EntityQuery prefabQuery;
        private bool initialized;

        protected override void OnCreate()
        {
            base.OnCreate();
            crowdSkinProviderSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CrowdSkinProviderSystem>();
            crowdSkinProviderSystem.OnInitialized += CrowdSkinProviderSystem_OnInitialized;

            prefabQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithPresent<GPUSkinTag>()
                .WithAll<Prefab, SkinAnimatorData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            RequireForUpdate(prefabQuery);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            crowdSkinProviderSystem.OnInitialized -= CrowdSkinProviderSystem_OnInitialized;
        }

        protected override void OnUpdate()
        {
            if (initialized)
            {
                Initialize();
                Enabled = false;
            }
        }

        private void Initialize()
        {
            var entities = prefabQuery.ToEntityArray(Allocator.TempJob);
            var renderMeshArray = crowdSkinProviderSystem.TotalRenderMeshData;

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var skinAnimatorData = EntityManager.GetComponentData<SkinAnimatorData>(entity);

                EntityManager.AddSharedComponentManaged(entity, renderMeshArray);

                var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

                materialMeshInfo.MeshID = crowdSkinProviderSystem.GetDefaultMeshBatchId(skinAnimatorData.SkinIndex);
                materialMeshInfo.MaterialID = crowdSkinProviderSystem.GetDefaultMaterialBatchId(skinAnimatorData.SkinIndex);

                EntityManager.SetComponentData(entity, materialMeshInfo);
            }

            entities.Dispose();
        }

        private void CrowdSkinProviderSystem_OnInitialized()
        {
            initialized = true;
        }
    }
}