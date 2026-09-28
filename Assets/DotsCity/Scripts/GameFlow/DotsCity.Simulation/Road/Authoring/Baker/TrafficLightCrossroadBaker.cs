using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Hash128 = Unity.Entities.Hash128;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [TemporaryBakingType]
    public struct LightCrossroadBakingData : IComponentData
    {
        public int UniqueID;
        public NativeArray<TrafficLightHandlerTempData> LightHandlerEntities;
        public NativeArray<LightStateInfo> LightStates;
    }

    [TemporaryBakingType]
    public struct CrossroadBakingData : IComponentData
    {
        public int InstanceId;
        public float3 Position;
        public int PositionHash;

        public Hash128 SceneHashCode;
        public int SectionIndex;

        public NativeArray<Entity> TrafficNodeScopes;
        public NativeArray<Entity> SubNodes;
        public bool HasInnerSubNodes;
        public bool HasSubNodes;
    }

    [TemporaryBakingType]
    public struct CrossroadSubSegmentTag : IComponentData { }

    public struct TrafficLightHandlerTempData
    {
        public Entity Entity;
        public int LightStateCount;
        public int HandlerIndex;
    }

    public class TrafficLightCrossroadBaker : Baker<TrafficLightCrossroad>
    {
        public override void Bake(TrafficLightCrossroad authoring)
        {
            var entity = BakerExtension.CreateAdditionalEntityWithBakerRef(this, authoring.gameObject, TransformUsageFlags.None, false);

            CrossroadBakingData crossroadBakingData = new CrossroadBakingData();

            crossroadBakingData.Position = authoring.transform.position;
            crossroadBakingData.InstanceId = authoring.GetInstanceID();
            crossroadBakingData.TrafficNodeScopes = new NativeArray<Entity>(authoring.TrafficNodes.Count, Allocator.Temp);
            crossroadBakingData.SubNodes = new NativeArray<Entity>(authoring.TrafficNodes.Count, Allocator.Temp);

            if (authoring.TrafficNodes.Count == 0)
            {
                UnityEngine.Debug.Log($"TrafficLightCrossroadBaker. Crossroad InstanceId {crossroadBakingData.InstanceId} doesn't have any traffic node. Make sure they are assigned to TrafficLightCrossroad{TrafficObjectFinderMessage.GetMessage()}");
            }

            bool hasSubNodes = false;
            bool hasInnerSubNodes = false;

            for (int i = 0; i < authoring.TrafficNodes.Count; i++)
            {
                if (authoring.TrafficNodes[i] == null)
                {
                    UnityEngine.Debug.Log($"{authoring.name} InstanceId {authoring.GetInstanceID()}. Traffic node is null. Select this segment to automatically clean up null nodes. {TrafficObjectFinderMessage.GetMessage()}");
                    continue;
                }

                crossroadBakingData.TrafficNodeScopes[i] = GetEntity(authoring.TrafficNodes[i].gameObject, TransformUsageFlags.None);

                if (!hasSubNodes)
                {
                    if (authoring.TrafficNodes[i].HasSubNodes)
                    {
                        hasSubNodes = true;
                        hasInnerSubNodes = authoring.TrafficNodes[i].SubNodeType.HasFlag(TrafficNodeSubNodeType.Inner);
                    }
                }
            }

            crossroadBakingData.HasSubNodes = hasSubNodes;
            crossroadBakingData.HasInnerSubNodes = hasInnerSubNodes;

            AddComponent<SegmentInitTag>(entity);
            AddComponent<SegmentComponent>(entity);
            AddBuffer<SegmentPedestrianNodeData>(entity);
            AddBuffer<SegmentTrafficNodeData>(entity);

            AddComponent(entity, crossroadBakingData);

            if (authoring.HasLights && authoring.enabled)
            {
                NativeList<LightStateInfo> states = new NativeList<LightStateInfo>(Allocator.Temp);

                LightCrossroadBakingData lightCrossroadBakingData = new LightCrossroadBakingData();

                lightCrossroadBakingData.UniqueID = authoring.UniqueId;
                var count = authoring.HandlerActiveCount;

                lightCrossroadBakingData.LightHandlerEntities = new NativeArray<TrafficLightHandlerTempData>(count, Allocator.Temp);
                int index = 0;

                foreach (var handlerData in authoring.TrafficLightHandlers)
                {
                    if (!handlerData.Value)
                    {
                        UnityEngine.Debug.Log($"Crossroad '{authoring.name}' InstanceID {authoring.GetInstanceID()} has null TrafficLightHandler.{TrafficObjectFinderMessage.GetMessage()}");
                        continue;
                    }

                    if (!handlerData.Value.gameObject.activeSelf)
                        continue;

                    var lightStateCount = ProcessHandlerLightStates(authoring, handlerData.Value, ref states);

                    lightCrossroadBakingData.LightHandlerEntities[index] = new TrafficLightHandlerTempData()
                    {
                        Entity = GetEntity(handlerData.Value.gameObject, TransformUsageFlags.None),
                        LightStateCount = lightStateCount,
                        HandlerIndex = handlerData.Key,
                    };

                    index++;
                }

                lightCrossroadBakingData.LightStates = states.ToArray(Allocator.Temp);

                AddComponent(entity, lightCrossroadBakingData);
                states.Dispose();
            }
        }

        private int ProcessHandlerLightStates(TrafficLightCrossroad authoring, TrafficLightHandler trafficLightHandler, ref NativeList<LightStateInfo> states)
        {
            int count = 0;
            var arrowSettings = authoring.TryToGetCustomArrow(trafficLightHandler);

            List<LightStateInfo> lightStates;

            if (arrowSettings == null || arrowSettings.relatedTrafficLightHandler == null)
            {
                if (trafficLightHandler.LightStates != null)
                {
                    lightStates = new List<LightStateInfo>(trafficLightHandler.LightStates);
                }
                else
                {
                    lightStates = new List<LightStateInfo>(0);
                }
            }
            else
            {
                lightStates = TrafficLightTimingUtils.GetArrowLightStates(arrowSettings);
            }

            if (lightStates == null || lightStates.Count == 0)
            {
                var crossRoadName = trafficLightHandler.TrafficLightCrossroad?.name ?? "Unknown crossroad";
                UnityEngine.Debug.Log($"Crossroad '{crossRoadName}' TrafficLightHandler InstanceID {trafficLightHandler.GetInstanceID()} '{trafficLightHandler.name}' HandlerIndex {trafficLightHandler.RelatedLightIndex} - 0 light states.{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (lightStates != null)
            {
                count = lightStates.Count;

                for (int i = 0; i < lightStates.Count; i++)
                {
                    states.Add(lightStates[i]);
                }
            }

            lightStates.Clear();
            lightStates = null;

            return count;
        }
    }
}