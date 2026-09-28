using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [TemporaryBakingType]
    public struct TrafficNodeLightTriggerBakingData : IComponentData
    {
        public Entity SourceLightEntity;
        public NativeArray<Entity> TargetLightEntities;
        public float TriggerDistance;
    }

    public class TrafficNodeLightTriggerAuthoring : MonoBehaviour
    {
        [Tooltip("Distance at which the trigger is deactivated")]
        public float TriggerDistance = 20f;

        [Tooltip("Traffic light that turn green when the trigger is activated")]
        public TrafficLightHandler sourceLight;

        [Tooltip("Traffic lights that turn red when the trigger is activated")]
        public List<TrafficLightHandler> relatedLights = new List<TrafficLightHandler>();

        class TrafficNodeLightTriggerAuthoringBaker : Baker<TrafficNodeLightTriggerAuthoring>
        {
            public override void Bake(TrafficNodeLightTriggerAuthoring authoring)
            {
                if (authoring.sourceLight == null)
                {
                    var node = authoring.GetComponent<TrafficNode>();
                    Debug.Log($"TrafficNodeLightTriggerAuthoring. TrafficNode InstanceID {node.GetInstanceID()}. Source light is null{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }

                if (authoring.relatedLights.Count == 0)
                {
                    var node = authoring.GetComponent<TrafficNode>();
                    Debug.Log($"TrafficNodeLightTriggerAuthoring. TrafficNode InstanceID {node.GetInstanceID()}. Related lights is empty{TrafficObjectFinderMessage.GetMessage()}");
                    return;
                }

                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                var lightEntities = new NativeList<Entity>(Allocator.Temp);

                foreach (var relatedLight in authoring.relatedLights)
                {
                    if (relatedLight == null)
                    {
                        var node = authoring.GetComponent<TrafficNode>();
                        Debug.Log($"TrafficNodeLightTriggerAuthoring. TrafficNode InstanceID {node.GetInstanceID()}. Related light is null{TrafficObjectFinderMessage.GetMessage()}");
                    }

                    var lightEntity = GetEntity(relatedLight.gameObject, TransformUsageFlags.Dynamic);
                    lightEntities.Add(lightEntity);
                }

                AddComponent(entity, new TrafficNodeLightTriggerBakingData()
                {
                    SourceLightEntity = GetEntity(authoring.sourceLight.gameObject, TransformUsageFlags.Dynamic),
                    TargetLightEntities = lightEntities.ToArray(Allocator.Temp),
                    TriggerDistance = authoring.TriggerDistance
                });

                lightEntities.Dispose();
            }
        }

        private void Reset()
        {
            var node = GetComponent<TrafficNode>();

            if (node)
            {
                sourceLight = node.TrafficLightHandler;

                foreach (var handler in node.TrafficLightCrossroad.TrafficLightHandlers)
                {
                    if (handler.Value == sourceLight) continue;

                    relatedLights.Add(handler.Value);
                }

                EditorSaver.SetObjectDirty(this);
            }
            else
            {
                Debug.Log("TrafficNodeLightTriggerAuthoring should be a child of TrafficNode");
            }
        }
    }
}