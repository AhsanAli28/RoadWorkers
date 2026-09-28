#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using Spirit604.Gameplay.Road.Debug;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeLightStateDebugger : ITrafficNodeDebugger
    {
        private const float PathOffset = 5f;
        private const float CircleSize = 0.5f;

        private EntityManager entityManager;
        private EntityQuery singletonQuery;

        public TrafficNodeLightStateDebugger(EntityManager entityManager)
        {
            this.entityManager = entityManager;
            singletonQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
        }

        public void Tick(Entity entity)
        {
            var trafficNode = entityManager.GetComponentData<TrafficNodeComponent>(entity);
            var pos = entityManager.GetComponentData<LocalToWorld>(entity).Position;

            var lightState = LightState.Uninitialized;

            if (trafficNode.LightEntity != Entity.Null && entityManager.HasComponent<LightHandlerComponent>(trafficNode.LightEntity))
            {
                var trafficLightHandler = entityManager.GetComponentData<LightHandlerComponent>(trafficNode.LightEntity);
                lightState = trafficLightHandler.State;
            }

            Gizmos.color = TrafficLightSceneColor.StateToColor(lightState);
            Gizmos.DrawWireSphere(pos, 1f);

            if (!Application.isPlaying) return;

            if (!entityManager.HasBuffer<PathConnectionElement>(entity)) return;

            var pathBuffer = entityManager.GetBuffer<PathConnectionElement>(entity);

            for (int i = 0; i < pathBuffer.Length; i++)
            {
                var lightEntity = pathBuffer[i].CustomLightEntity;

                if (lightEntity != Entity.Null && entityManager.HasComponent<LightHandlerComponent>(lightEntity))
                {
                    var pathGraph = singletonQuery.GetSingleton<PathGraphSystem.Singleton>();
                    var trafficLightHandler = entityManager.GetComponentData<LightHandlerComponent>(lightEntity);
                    lightState = trafficLightHandler.State;
                    var pathPos = PathGraphExtension.GetPositionOnRoad(in pathGraph, pathBuffer[i].GlobalPathIndex, PathOffset);

                    Gizmos.color = TrafficLightSceneColor.StateToColor(lightState);
                    Gizmos.DrawWireSphere(pathPos, CircleSize);
                }
            }
        }
    }
}
#endif