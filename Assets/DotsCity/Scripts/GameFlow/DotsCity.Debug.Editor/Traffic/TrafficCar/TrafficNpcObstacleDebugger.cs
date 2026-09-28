using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficNpcObstacleDebugger : MonoBehaviourBase
    {

#pragma warning disable 0414

        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color areaColor = Color.white;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private int selectedIndex = -1;

#pragma warning restore 0414

#if UNITY_EDITOR

        private TrafficDebuggerSystem trafficDebuggerSystem;
        private EntityManager entityManager;
        private static bool isInitialized;
        private Entity debugEntity;

        private void Start()
        {
            trafficDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficDebuggerSystem>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void OnEnable()
        {
            PedestrianEntitySpawnerSystem.OnInitialized += PedestrianEntitySpawnerSystem_OnInitialized;
        }

        private void OnDisable()
        {
            PedestrianEntitySpawnerSystem.OnInitialized -= PedestrianEntitySpawnerSystem_OnInitialized;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (!enableDebug)
            {
                if (debugEntity != Entity.Null)
                {
                    entityManager.DestroyEntity(debugEntity);
                    debugEntity = Entity.Null;
                }

                return;
            }

            if (debugEntity == Entity.Null)
            {
                debugEntity = entityManager.CreateEntity(typeof(TrafficNpcObstacleConfigDebug));
            }

            if (selectedIndex <= -1)
            {
                for (int i = 0; i < trafficDebuggerSystem.Traffics.Length; i++)
                {
                    var entity = trafficDebuggerSystem.Traffics[i].Entity;

                    DrawDebug(entity, areaColor);
                }
            }
            else
            {
                if (trafficDebuggerSystem.TryToGetEntity(selectedIndex, out var trafficEntity))
                {
                    DrawDebug(trafficEntity, areaColor);
                }
            }
        }

        public static void DrawDebug(Entity selectedEntity, Color areaColor, bool gizmos = true)
        {
            if (!isInitialized)
                return;

            TrafficNpcCalculateObstacleSystem.AreaInfo item;

            if (TrafficNpcCalculateObstacleSystem.DebugAreaInfoStaticRef.TryGetValue(selectedEntity, out item))
            {
                if (gizmos)
                {
                    var oldColor = Gizmos.color;
                    Gizmos.color = areaColor;
                    Gizmos.DrawLine(item.LeftTopPoint, item.LeftTopPoint2);
                    Gizmos.DrawLine(item.LeftTopPoint2, item.RightTopPoint2);
                    Gizmos.DrawLine(item.RightTopPoint2, item.RightTopPoint);
                    Gizmos.DrawLine(item.RightTopPoint, item.LeftTopPoint);
                    Gizmos.color = oldColor;
                }
                else
                {
                    var oldColor = Handles.color;
                    Handles.color = areaColor;
                    Handles.DrawLine(item.LeftTopPoint, item.LeftTopPoint2);
                    Handles.DrawLine(item.LeftTopPoint2, item.RightTopPoint2);
                    Handles.DrawLine(item.RightTopPoint2, item.RightTopPoint);
                    Handles.DrawLine(item.RightTopPoint, item.LeftTopPoint);
                    Handles.color = oldColor;
                }
            }

            var hashMap = TrafficNpcCalculateObstacleSystem.DebugObstacleNpcMultiHashMapStaticRef;

            if (hashMap.IsCreated)
            {
                if (hashMap.TryGetFirstValue(selectedEntity, out var npcObstacleInfo, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        var color = npcObstacleInfo.IsObstacle == 1 ? Color.red : Color.white;

                        if (gizmos)
                        {
                            Gizmos.color = color;
                            Gizmos.DrawWireSphere(npcObstacleInfo.Position, 1f);
                        }
                        else
                        {
                            Handles.color = color;
                            Handles.DrawWireDisc(npcObstacleInfo.Position, Vector3.up, 1f);
                        }

                    } while (hashMap.TryGetNextValue(out npcObstacleInfo, ref nativeMultiHashMapIterator));
                }
            }
        }

        private void PedestrianEntitySpawnerSystem_OnInitialized()
        {
            isInitialized = true;
        }

#endif

    }
}
