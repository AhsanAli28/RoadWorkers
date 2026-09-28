using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.CameraService;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Events
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class CameraShakeEventSystem : BeginSimulationSystemBase
    {
        private const float MAX_SHAKE_DISTANCE_SQ = 15f * 15f;

        private CameraController cameraController;
        private EntityQuery playerQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerTag, LocalTransform>()
                .Build();

            RequireForUpdate<PlayerTag>();
            RequireForUpdate<CameraShakeEventData>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            var entity = playerQuery.GetSingletonEntity();
            var playerPos = EntityManager.GetComponentData<LocalTransform>(entity).Position;

            Entities
            .WithoutBurst()
            .ForEach((Entity entity, in CameraShakeEventData cameraShakeCommand) =>
            {
                float distance = math.distancesq(cameraShakeCommand.Position, playerPos);

                if (distance < MAX_SHAKE_DISTANCE_SQ)
                {
#if CINEMACHINE
                    cameraController?.ActivateShake();
#endif
                }

                commandBuffer.DestroyEntity(entity);
            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(CameraController cameraController)
        {
            this.cameraController = cameraController;
        }
    }
}