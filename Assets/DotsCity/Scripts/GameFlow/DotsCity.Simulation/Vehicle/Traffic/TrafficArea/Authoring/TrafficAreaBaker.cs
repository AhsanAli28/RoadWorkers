using Spirit604.Gameplay.Road;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea.Authoring
{
    [TemporaryBakingType]
    public struct TrafficAreaBakerTempBakingData : IComponentData
    {
        public Entity RelatedSegment;
        public NativeArray<TrafficAreaAuthoring.NodePlaceType> PlaceNodeTypes;
        public NativeArray<Entity> EnterNodes;
        public NativeArray<Entity> QueueNodes;
        public NativeArray<Entity> ExitNodes;
        public NativeArray<Entity> DefaultNodes;
    }

    public class TrafficAreaBaker : Baker<TrafficAreaAuthoring>
    {
        public override void Bake(TrafficAreaAuthoring trafficArea)
        {
            var entity = CreateAdditionalEntity(TransformUsageFlags.None);

            AddComponent(entity, new TrafficAreaTag());
            AddComponent(entity, new TrafficAreaComponent()
            {
                MaxEntryQueueCount = trafficArea.MaxQueueCount,
                MaxSkipOrderCount = trafficArea.MaxSkipEnterOrderCount,
            });

            if (trafficArea.HasExitOrder)
            {
                AddComponent(entity, typeof(TrafficAreaHasExitParkingOrderTag));
            }

            // IEnableable components
            AddComponent(entity, typeof(TrafficAreaProcessingEnterQueueTag));
            AddComponent(entity, typeof(TrafficAreaProcessingExitQueueTag));
            AddComponent(entity, typeof(TrafficAreaUpdateLockStateTag));
            AddComponent(entity, typeof(TrafficAreaCarObserverEnabledTag));

            AddBuffer<TrafficAreaEnterCarQueueElement>(entity);
            AddBuffer<TrafficAreaExitCarQueueElement>(entity);
            AddBuffer<TrafficAreaEnterNodeElement>(entity);
            AddBuffer<TrafficAreaQueueNodeElement>(entity);

            var trafficAreaBakerTempBakingData = new TrafficAreaBakerTempBakingData();

            var enterNodes = trafficArea.TryToGetNodes(TrafficAreaNodeType.Enter);

            if (enterNodes?.Count > 0)
            {
                trafficAreaBakerTempBakingData.EnterNodes = new NativeArray<Entity>(enterNodes.Count, Allocator.Temp);

                for (int i = 0; i < enterNodes.Count; i++)
                {
                    if (enterNodes[i] == null)
                    {
                        UnityEngine.Debug.Log($"Traffic Area {trafficArea.name}. Index {i} enter node is null");
                        continue;
                    }

                    AddSegmentLink(ref trafficAreaBakerTempBakingData, enterNodes[i]);
                    trafficAreaBakerTempBakingData.EnterNodes[i] = GetEntity(enterNodes[i].gameObject, TransformUsageFlags.Dynamic);
                }
            }
            else
            {
                DrawWarning(trafficArea, TrafficAreaNodeType.Enter);
            }

            var queueNodes = trafficArea.TryToGetNodes(TrafficAreaNodeType.Queue);

            if (queueNodes?.Count > 0)
            {
                trafficAreaBakerTempBakingData.QueueNodes = new NativeArray<Entity>(queueNodes.Count, Allocator.Temp);

                for (int i = 0; i < queueNodes.Count; i++)
                {
                    if (queueNodes[i] == null)
                    {
                        UnityEngine.Debug.Log($"Traffic Area {trafficArea.name}. Index {i} queue node is null");
                        continue;
                    }

                    AddSegmentLink(ref trafficAreaBakerTempBakingData, queueNodes[i]);
                    trafficAreaBakerTempBakingData.QueueNodes[i] = GetEntity(queueNodes[i].gameObject, TransformUsageFlags.Dynamic);
                }
            }
            else
            {
                DrawWarning(trafficArea, TrafficAreaNodeType.Queue);
            }

            var exitNodes = trafficArea.TryToGetNodes(TrafficAreaNodeType.Exit);

            if (exitNodes?.Count > 0)
            {
                trafficAreaBakerTempBakingData.ExitNodes = new NativeArray<Entity>(exitNodes.Count, Allocator.Temp);

                for (int i = 0; i < exitNodes.Count; i++)
                {
                    if (exitNodes[i] == null)
                    {
                        UnityEngine.Debug.Log($"Traffic Area {trafficArea.name}. Index {i} exit node is null");
                        continue;
                    }

                    AddSegmentLink(ref trafficAreaBakerTempBakingData, exitNodes[i]);
                    trafficAreaBakerTempBakingData.ExitNodes[i] = GetEntity(exitNodes[i].gameObject, TransformUsageFlags.Dynamic);
                }
            }
            else
            {
                DrawWarning(trafficArea, TrafficAreaNodeType.Exit);
            }

            var defaultNodes = trafficArea.TryToGetNodes(TrafficAreaNodeType.Default);

            if (defaultNodes?.Count > 0)
            {
                trafficAreaBakerTempBakingData.DefaultNodes = new NativeArray<Entity>(defaultNodes.Count, Allocator.Temp);

                for (int i = 0; i < defaultNodes.Count; i++)
                {
                    if (defaultNodes[i] == null)
                    {
                        UnityEngine.Debug.Log($"Traffic Area {trafficArea.name}. Index {i} default node is null");
                        continue;
                    }

                    AddSegmentLink(ref trafficAreaBakerTempBakingData, defaultNodes[i]);
                    trafficAreaBakerTempBakingData.DefaultNodes[i] = GetEntity(defaultNodes[i].gameObject, TransformUsageFlags.Dynamic);
                }
            }
            else
            {
                DrawWarning(trafficArea, TrafficAreaNodeType.Default);
            }

            var values = Enum.GetValues(typeof(TrafficAreaNodeType)).Cast<TrafficAreaNodeType>().ToArray();

            trafficAreaBakerTempBakingData.PlaceNodeTypes = new NativeArray<TrafficAreaAuthoring.NodePlaceType>(values.Length, Allocator.Temp);

            for (int i = 0; i < values.Length; i++)
            {
                trafficAreaBakerTempBakingData.PlaceNodeTypes[i] = trafficArea.GetNodePlace(values[i]);
            }

            AddComponent(entity, trafficAreaBakerTempBakingData);
        }

        private void AddSegmentLink(ref TrafficAreaBakerTempBakingData trafficAreaBakerTempBakingData, TrafficNode trafficNode)
        {
            if (trafficAreaBakerTempBakingData.RelatedSegment == Entity.Null && trafficNode && trafficNode.TrafficLightCrossroad)
            {
                trafficAreaBakerTempBakingData.RelatedSegment = GetEntity(trafficNode.TrafficLightCrossroad.gameObject, TransformUsageFlags.Dynamic);
            }
        }

        private static void DrawWarning(TrafficAreaAuthoring trafficArea, TrafficAreaNodeType trafficAreaNodeType)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"TrafficArea '{trafficArea.name}' doesn't have {trafficAreaNodeType} nodes");
#endif
        }
    }
}