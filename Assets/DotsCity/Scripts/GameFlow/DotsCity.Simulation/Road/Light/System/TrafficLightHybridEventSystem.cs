using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [RequireMatchingQueriesForUpdate]
    public partial class TrafficLightHybridEventSystem : SystemBase
    {
        private TrafficLightHybridService trafficLightHybridService;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<LightHandlerStateUpdateTag>()
            .ForEach((
                Entity entity,
                in LightHandlerID handlerID,
                in LightHandlerComponent lightHandlerComponent) =>
            {
                trafficLightHybridService.UpdateState(handlerID.Value, lightHandlerComponent.State);
                EntityManager.SetComponentEnabled<LightHandlerStateUpdateTag>(entity, false);
            }).Run();
        }

        public void Initialize(TrafficLightHybridService trafficLightHybridService)
        {
            this.trafficLightHybridService = trafficLightHybridService;
            Enabled = true;
        }
    }
}
