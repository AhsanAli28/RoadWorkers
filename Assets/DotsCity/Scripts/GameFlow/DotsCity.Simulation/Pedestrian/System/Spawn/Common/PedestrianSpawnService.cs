using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Spirit604.DotsCity.Simulation.Pedestrian.PedestrianSpawnQueueSystem;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianSpawnService : SingletonMonoBehaviour<PedestrianSpawnService>
    {
        private EntityQuery eventQueueQuery;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private void Awake()
        {
            base.Awake();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianSpawnQueueSystem, StructuralSystemGroup>(true);
            // Force the system’s OnCreate to run immediately:
            World.DefaultGameObjectInjectionWorld.Update();
            eventQueueQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnQueueSystem.Singleton>());

            //DefaultWorldUtils.CreateAndAddSystemUnmanaged<PedestrianSpawnQueueSystem, SimulationSystemGroup>(true);
            //World.DefaultGameObjectInjectionWorld.Update();  // now OnCreate for your system will fire
            //eventQueueQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PedestrianSpawnQueueSystem.Singleton>());
        }

        public void Spawn(Entity nodeEntity, int skinIndex = -1)
        {
            if (eventQueueQuery.TryGetSingleton<PedestrianSpawnQueueSystem.Singleton>(out var queue))
                queue.AddSpawnEvent(nodeEntity, skinIndex);
            else
                Debug.LogWarning("SpawnQueue singleton not ready yet");

            //var eventQueue = eventQueueQuery.Try<PedestrianSpawnQueueSystem.Singleton>();
            //eventQueue.AddSpawnEvent(nodeEntity, skinIndex);
        }
    }
}

