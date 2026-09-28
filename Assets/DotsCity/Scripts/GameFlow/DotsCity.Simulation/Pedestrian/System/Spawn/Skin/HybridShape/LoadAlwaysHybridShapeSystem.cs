using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class LoadAlwaysHybridShapeSystem : BeginSimulationSystemBase
    {
        private const float LoadSkinFrequency = 0.4f;

        private HybridShapeFactory hybridShapeFactory;
        private List<NpcHybridEntityRef> entities = new List<NpcHybridEntityRef>(100);

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            float timestamp = (float)SystemAPI.Time.ElapsedTime;

            entities.Clear();

            Entities
            .WithoutBurst()
            .WithDisabled<HybridShapeTag>()
            .WithAll<DisableUnloadSkinTag>()
            .ForEach((
                Entity pedestrianEntity,
                ref DestinationComponent destinationComponent,
                ref PedestrianCommonSettings pedestrianCommonSettings,
                in LocalTransform transform) =>
            {
                bool shouldLoad = (timestamp - pedestrianCommonSettings.LoadSkinTimestamp) >= LoadSkinFrequency;

                if (shouldLoad)
                {
                    pedestrianCommonSettings.LoadSkinTimestamp = timestamp;

                    var pedestrianSkin = hybridShapeFactory.Get().GetComponent<NpcHybridEntityRef>();

                    if (pedestrianSkin != null)
                    {
                        pedestrianSkin.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                        pedestrianSkin.Initialize(pedestrianEntity, EntityManager);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogError($"LoadAlwaysHybridShapeSystem. NpcHybridEntityRef is null.");
#endif
                    }

                    entities.Add(pedestrianSkin);

                    commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(pedestrianEntity, true);
                    commandBuffer.SetComponentEnabled<HybridShapeTag>(pedestrianEntity, true);

                    commandBuffer.SetSharedComponent(pedestrianEntity, new WorldEntitySharedType(EntityWorldType.HybridEntity));
                }
            }).Run();

            for (int i = 0; i < entities.Count; i++)
            {
                var skinData = entities[i];

                if (skinData)
                {
                    EntityManager.AddComponentObject(skinData.RelatedEntity, skinData.transform);
                }
            }

            AddCommandBufferForProducer();
        }

        public void Initialize(HybridShapeFactory hybridShapeFactory)
        {
            this.hybridShapeFactory = hybridShapeFactory;
            Enabled = true;
        }
    }
}