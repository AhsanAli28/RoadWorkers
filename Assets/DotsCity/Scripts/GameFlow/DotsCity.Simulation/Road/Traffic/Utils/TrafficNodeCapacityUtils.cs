using Spirit604.DotsCity.Simulation.Traffic;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public static class TrafficNodeCapacityUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LinkNode(ref this TrafficNodeCapacityComponent trafficNodeCapacityComponent, Entity carEntity)
        {
            if (trafficNodeCapacityComponent.Capacity > 0)
            {
                trafficNodeCapacityComponent.Capacity--;
                trafficNodeCapacityComponent.CarEntity = carEntity;
                return true;
            }

            if (trafficNodeCapacityComponent.CarEntity == carEntity)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AlreadyLinked(ref this TrafficNodeCapacityComponent trafficNodeCapacityComponent, Entity carEntity) => trafficNodeCapacityComponent.CarEntity == carEntity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanLinkNode(ref this TrafficNodeCapacityComponent trafficNodeCapacityComponent, Entity carEntity) => trafficNodeCapacityComponent.Capacity > 0 && trafficNodeCapacityComponent.CarEntity == Entity.Null || trafficNodeCapacityComponent.AlreadyLinked(carEntity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UnlinkNode(ref this TrafficNodeCapacityComponent trafficNodeCapacityComponent)
        {
            if (trafficNodeCapacityComponent.HasCar())
            {
                if (trafficNodeCapacityComponent.Capacity >= 0)
                {
                    trafficNodeCapacityComponent.Capacity++;
                }

                trafficNodeCapacityComponent.CarEntity = Entity.Null;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UnlinkNodeAndReqDriver(ref this TrafficNodeCapacityComponent trafficNodeCapacityComponent, ref EntityCommandBuffer commandBuffer, bool ignorePedestrian = false)
        {
            if (!trafficNodeCapacityComponent.HasCar())
                return false;

            if (trafficNodeCapacityComponent.Capacity >= 0)
            {
                trafficNodeCapacityComponent.Capacity++;
            }

            commandBuffer.AddComponent<ParkingDriverRequestTag>(trafficNodeCapacityComponent.CarEntity);

            trafficNodeCapacityComponent.CarEntity = Entity.Null;

            if (trafficNodeCapacityComponent.PedestrianNodeEntity != Entity.Null)
            {
            }
            else if (!ignorePedestrian)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log("Pedestrian parking linked entity not found");
#endif
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryToLinkNode(
            Entity entity,
            Entity enteredNodeEntity,
            ref EntityCommandBuffer commandBuffer,
            ref ComponentLookup<TrafficNodeCapacityComponent> trafficNodeCapacityLookup,
            in ComponentLookup<TrafficNodeSettingsComponent> trafficNodeSettingsLookup,
            in TrafficRoadConfigReference trafficRoadConfigReference)
        {
            var capacityChanged = false;
            var linked = false;
            var settingsComponent = trafficNodeSettingsLookup[enteredNodeEntity];
            bool isLinkedNode = (trafficRoadConfigReference.Config.Value.LinkedNodeFlags & settingsComponent.TrafficNodeTypeFlag) != 0;
            var trafficNodeCapacity = trafficNodeCapacityLookup[enteredNodeEntity];

            if (isLinkedNode)
            {
                if (trafficNodeCapacity.AlreadyLinked(entity))
                {
                    linked = true;
                }
                else if (trafficNodeCapacity.LinkNode(entity))
                {
                    capacityChanged = true;
                    linked = true;

                    var trafficNodeLinkedComponent = new TrafficNodeLinkedComponent()
                    {
                        LinkedPlace = enteredNodeEntity
                    };

                    commandBuffer.AddComponent(entity, trafficNodeLinkedComponent);
                }
            }

            if (capacityChanged)
            {
                trafficNodeCapacityLookup[enteredNodeEntity] = trafficNodeCapacity;
            }

            return linked;
        }
    }
}
