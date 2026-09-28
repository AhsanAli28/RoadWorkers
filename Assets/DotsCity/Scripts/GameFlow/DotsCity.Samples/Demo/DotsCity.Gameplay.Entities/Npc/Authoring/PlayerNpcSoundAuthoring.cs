using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Sound.Utils;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Sound.Player.Authoring
{
    public class PlayerNpcSoundAuthoring : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonConfigs.html#player-npc-sound-config")]
        [SerializeField] private string link;

#pragma warning disable 0414

        [SerializeField] private SoundData footstepsSoundData;

        [SerializeField][Range(0.01f, 1f)] private float footstepFrequency = 0.3f;

#pragma warning restore 0414

        class PlayerNpcSoundAuthoringBaker : Baker<PlayerNpcSoundAuthoring>
        {
            public override void Bake(PlayerNpcSoundAuthoring authoring)
            {
                if (authoring.footstepsSoundData == null)
                {
                    UnityEngine.Debug.Log($"PlayerNpcSoundAuthoring sound data is missing");
                    return;
                }

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<PlayerNpcSoundConfig>();

                    root.FootstepsSoundId = authoring.footstepsSoundData.Id;
                    root.FootstepFrequency = authoring.footstepFrequency;

                    var blobRef = builder.CreateBlobAssetReference<PlayerNpcSoundConfig>(Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new PlayerNpcSoundConfigReference() { Config = blobRef });
                }
            }
        }
    }

    [UpdateInGroup(typeof(StructuralInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerInitSoundSystem : BeginSimulationSystemBase
    {
        private EntityQuery playerUpdateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerUpdateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<PlayerSoundComponent>()
                .WithAll<PlayerTag, AnimatorMovementComponent>()
                .Build(this);

            RequireForUpdate(playerUpdateQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();
            float currentTimestamp = (float)SystemAPI.Time.ElapsedTime;

            var config = SystemAPI.GetSingleton<PlayerNpcSoundConfigReference>();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithNone<PlayerSoundComponent>()
            .WithAll<PlayerTag, AnimatorMovementComponent>()
            .ForEach((
                Entity entity) =>
            {
                var entityManager = EntityManager;
                var soundId = config.Config.Value.FootstepsSoundId;
                var soundEntity = entityManager.CreateChildSoundEntity(soundId, entity);

                var playerSoundComponent = new PlayerSoundComponent()
                {
                    FootstepsSound = soundEntity,
                    FootstepsPlaying = true,
                    FootstepFrequency = config.Config.Value.FootstepFrequency,
                };

                commandBuffer.AddComponent(entity, playerSoundComponent);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
