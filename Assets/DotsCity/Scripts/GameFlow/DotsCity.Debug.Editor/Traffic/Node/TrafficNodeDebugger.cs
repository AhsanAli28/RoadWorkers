using Spirit604.DotsCity.Simulation.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNodeDebugger : MonoBehaviour
    {
        private enum TrafficNodeDebuggerType { Disabled, Index, Available, LightState, Capacity, Streaming }

        [SerializeField] private TrafficNodeDebuggerType trafficNodeDebuggerType;

#if UNITY_EDITOR

        private Dictionary<TrafficNodeDebuggerType, ITrafficNodeDebugger> debuggers = new Dictionary<TrafficNodeDebuggerType, ITrafficNodeDebugger>();
        private EntityQuery trafficNodeQuery;
        private bool playMode;

        private void Awake()
        {
            Init();
        }

        private bool Init()
        {
            if (debuggers.Count != 0 && Application.isPlaying == playMode)
                return true;

            if (World.DefaultGameObjectInjectionWorld == null)
                return false;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            trafficNodeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeSettingsComponent>());

            playMode = Application.isPlaying;
            debuggers.Clear();
            debuggers.Add(TrafficNodeDebuggerType.Index, new TrafficNodeCommonDebugger(entityManager));
            debuggers.Add(TrafficNodeDebuggerType.Available, new TrafficNodeAvailableDebugger(entityManager));
            debuggers.Add(TrafficNodeDebuggerType.LightState, new TrafficNodeLightStateDebugger(entityManager));
            debuggers.Add(TrafficNodeDebuggerType.Capacity, new TrafficNodeCapacityDebugger(entityManager));
            debuggers.Add(TrafficNodeDebuggerType.Streaming, new TrafficNodeStreamingDebugger(entityManager));
            return true;
        }

        private void OnDrawGizmos()
        {
            if (trafficNodeDebuggerType == TrafficNodeDebuggerType.Disabled)
                return;

            if (!Init())
                return;

            var entities = trafficNodeQuery.ToEntityArray(Allocator.TempJob);

            if (debuggers.TryGetValue(trafficNodeDebuggerType, out var debugger))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    debugger.Tick(entity);
                }
            }

            entities.Dispose();
        }
#endif
    }
}
