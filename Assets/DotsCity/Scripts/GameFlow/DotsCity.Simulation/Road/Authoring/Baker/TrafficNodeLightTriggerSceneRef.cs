using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Binding;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class TrafficNodeLightTriggerSceneRef : MonoBehaviour
    {
        [SerializeField]
        private EntityWeakRef trafficNodeSceneRef;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private void OnEnable()
        {
            trafficNodeSceneRef.SubscribeBinding();
        }
        private void OnDisable()
        {
            trafficNodeSceneRef.UnsubscribeBinding();
        }

        public void AddEntity(IHybridEntityRef hybridEntityRef)
        {
            EntityManager.SetComponentEnabled<LightTriggerEnabledTag>(trafficNodeSceneRef.Entity, true);
            EntityManager.SetComponentData(trafficNodeSceneRef.Entity, new LightTriggerBinding()
            {
                TriggerEntity = hybridEntityRef.RelatedEntity
            });
        }
    }
}