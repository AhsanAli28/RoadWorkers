using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    public partial class PedestrianLocalSpawnerInitSystem : BeginSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                in PedestrianLocalSpawnerDataComponent pedestrianSpawnerDataComponent) =>
            {
                GameObject localSpawner = null;
                var instanceId = pedestrianSpawnerDataComponent.LocalSpawnerInstanceId;

#if UNITY_EDITOR
                var localSpawnerObj = EditorUtility.InstanceIDToObject(pedestrianSpawnerDataComponent.LocalSpawnerInstanceId);

                if (localSpawnerObj != null)
                {
                    localSpawner = localSpawnerObj as GameObject;
                }
#else
                // localSpawner = Extensions.ObjectUtils.FindObjectsOfType<GameObject>().Where(a => a.GetInstanceID() == instanceId).FirstOrDefault();
#endif

                if (localSpawner != null)
                {
                    var pedestrianDebugLocalSpawner = localSpawner.GetComponent<PedestrianLocalSpawner>();
                    pedestrianDebugLocalSpawner.InitEntity(entity, pedestrianSpawnerDataComponent.LocalIndex);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"PedestrianLocalSpawner InstanceId {pedestrianSpawnerDataComponent.LocalSpawnerInstanceId} not found. Make sure, that entity subscene is opened at editor time.");
#endif
                }

                commandBuffer.RemoveComponent<PedestrianLocalSpawnerDataComponent>(entity);

            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
