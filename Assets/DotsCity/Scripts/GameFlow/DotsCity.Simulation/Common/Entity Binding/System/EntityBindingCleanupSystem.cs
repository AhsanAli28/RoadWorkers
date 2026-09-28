using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Binding
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class EntityBindingCleanupSystem : BeginSimulationSystemBase
    {
        private EntityBindingService entityBindingService;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithNone<EntityID>()
            .ForEach((
                Entity entity,
                in EntityBindingCleanup bindingCleanup) =>
            {
                entityBindingService.UnregisterEntity(bindingCleanup.Value);

                commandBuffer.RemoveComponent<EntityBindingCleanup>(entity);
            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(EntityBindingService entityBindingService)
        {
            this.entityBindingService = entityBindingService;
            Enabled = true;
        }
    }
}