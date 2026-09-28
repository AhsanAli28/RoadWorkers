using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateInGroup(typeof(BeginSimulationGroup))]
    public partial class TrafficLightHybridDataSystem : SystemBase
    {
        private NativeHashMap<int, Entity> entityBinding;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (entityBinding.IsCreated) entityBinding.Dispose();
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<LightHandlerInitTag>()
            .ForEach((
                Entity entity,
                in LightHandlerID handlerID) =>
            {
                if (!entityBinding.ContainsKey(handlerID.Value))
                {
                    entityBinding.Add(handlerID.Value, entity);
                }
                else
                {
                    entityBinding[handlerID.Value] = entity;
                }
            }).Run();
        }

        public bool SetForceState(int id, LightState lightState)
        {
            if (entityBinding.TryGetValue(id, out var lightEntity))
            {
                if (EntityManager.HasComponent<LightHandlerComponent>(lightEntity))
                {
                    if (!EntityManager.HasComponent<LightHandlerOverrideStateTag>(lightEntity))
                    {
                        EntityManager.AddComponent<LightHandlerOverrideStateTag>(lightEntity);
                    }

                    var lightHandlerComponent = EntityManager.GetComponentData<LightHandlerComponent>(lightEntity);

                    lightHandlerComponent.State = lightState;

                    EntityManager.SetComponentData(lightEntity, lightHandlerComponent);
                    EntityManager.SetComponentEnabled<LightHandlerStateUpdateTag>(lightEntity, true);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveForceState(int id)
        {
            if (entityBinding.TryGetValue(id, out var lightEntity))
            {
                if (EntityManager.HasComponent<LightHandlerOverrideStateTag>(lightEntity))
                {
                    EntityManager.RemoveComponent<LightHandlerOverrideStateTag>(lightEntity);
                    EntityManager.SetComponentEnabled<LightHandlerInitTag>(lightEntity, true);
                    return true;
                }
            }

            return false;
        }

        public void Initialize()
        {
            entityBinding = new NativeHashMap<int, Entity>(200, Allocator.Persistent);
            Enabled = true;
        }
    }
}
