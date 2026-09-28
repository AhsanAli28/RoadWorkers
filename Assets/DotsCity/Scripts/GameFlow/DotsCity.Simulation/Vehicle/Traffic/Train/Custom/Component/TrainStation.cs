using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    public class TrainStation : MonoBehaviour
    {
        [SerializeField]
        private EntityWeakRef trainStationNode;

        private Entity trainEntity;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public event Action<TrainStation> TrainCompleted = delegate { };

        private void Awake()
        {
            trainStationNode.SubscribeBinding();
            enabled = false;
        }

        private void OnDestroy()
        {
            trainStationNode.UnsubscribeBinding();
        }

        private void Update()
        {
            if (trainEntity != Entity.Null && !EntityManager.HasComponent<TrafficPublicIdleComponent>(trainEntity))
            {
                TrainCompleted(this);
                enabled = false;
            }
        }

        public void Activate(IHybridEntityRef trainEntityRef)
        {
            trainEntity = trainEntityRef.RelatedEntity;
            var enteredNodeEntity = trainStationNode.Entity;

            if (enteredNodeEntity.Index == 0)
            {
                Debug.Log($"TrainStation '{name}'. Entity is null, make sure that all scene entities are loaded & appropriate entity is assigned");
                return;
            }

            var trafficNodeCapacityComponent = EntityManager.GetComponentData<TrafficNodeCapacityComponent>(enteredNodeEntity);

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            trafficNodeCapacityComponent.LinkNode(trainEntity);

            commandBuffer.AddComponent(trainEntity, new TrafficNodeLinkedComponent()
            {
                LinkedPlace = enteredNodeEntity
            });

            commandBuffer.SetComponent(enteredNodeEntity, trafficNodeCapacityComponent);

            var trafficStateComponent = EntityManager.GetComponentData<TrafficStateComponent>(trainEntity);

            TrafficStateExtension.AddIdleState<TrafficPublicIdleComponent>(ref commandBuffer, trainEntity, ref trafficStateComponent, TrafficIdleState.PublicTransportStop);

            commandBuffer.SetComponent(trainEntity, trafficStateComponent);

            if (EntityManager.HasBuffer<ConnectedPedestrianNodeElement>(enteredNodeEntity))
            {
                var buffer = EntityManager.GetBuffer<ConnectedPedestrianNodeElement>(enteredNodeEntity);

                for (int i = 0; i < buffer.Length; i++)
                {
                    var pedestrianNodeEntity = buffer[i].PedestrianNodeEntity;

                    if (pedestrianNodeEntity == Entity.Null) continue;

                    bool switchState = true;

#if UNITY_EDITOR
                    if (!EntityManager.HasComponent<NodeProcessWaitQueueTag>(pedestrianNodeEntity))
                    {
                        Debug.Log($"TrainStation {name}. PedestrianNode entity {pedestrianNodeEntity.Index} has wrong pedestrian node type. Make sure entity has 'TrafficPublicStopStation' type");
                        switchState = false;
                    }
#endif

                    if (switchState)
                        commandBuffer.SetComponentEnabled<NodeProcessWaitQueueTag>(pedestrianNodeEntity, true);

                }
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
            enabled = true;
        }

        public void Deactivate()
        {
            enabled = false;
            trainEntity = default;
        }
    }
}