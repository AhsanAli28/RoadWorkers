using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class LoadAlwaysHybridSkinSystem : BeginSimulationSystemBase
    {
        private const float LoadSkinFrequency = 0.4f;

        private PedestrianSkinFactory pedestrianSkinFactory;
        private List<PedestrianEntityRef> entities = new List<PedestrianEntityRef>(100);

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
            .WithDisabled<HasSkinTag>()
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

                    var pedestrianSkin = pedestrianSkinFactory.SpawnSkin(pedestrianCommonSettings.SkinIndex).GetComponent<PedestrianEntityRef>();

                    if (pedestrianSkin != null)
                    {
                        pedestrianSkin.Transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                        pedestrianSkin.Initialize(pedestrianEntity, EntityManager);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.LogError($"LoadHybridSkinSystem. Pedestrian SkinIndex '{pedestrianCommonSettings.SkinIndex}'. Each pedestrian skin must have a PedestrianEntityRef component.");
#endif
                    }

                    entities.Add(pedestrianSkin);

                    commandBuffer.SetComponentEnabled<MovementStateChangedEventTag>(pedestrianEntity, true);
                    commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(pedestrianEntity, true);
                    commandBuffer.SetComponentEnabled<HasSkinTag>(pedestrianEntity, true);

                    commandBuffer.SetSharedComponent(pedestrianEntity, new WorldEntitySharedType(EntityWorldType.HybridEntity));
                }
            }).Run();

            for (int i = 0; i < entities.Count; i++)
            {
                var skinData = entities[i];

                if (skinData)
                {
                    EntityManager.AddComponentObject(skinData.RelatedEntity, skinData.Transform);
                    EntityManager.AddComponentObject(skinData.RelatedEntity, skinData.Animator);
                }
            }

            AddCommandBufferForProducer();
        }

        public void Initialize(PedestrianSkinFactory pedestrianSkinFactory)
        {
            this.pedestrianSkinFactory = pedestrianSkinFactory;
            Enabled = true;
        }
    }
}