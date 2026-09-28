using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficCarRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<TrafficTag>(),
                ComponentType.ReadOnly<AliveTag>(),
                ComponentType.ReadOnly<CarTag>(),
                ComponentType.ReadOnly<TrafficStateComponent>(),
                ComponentType.ReadOnly<TrafficIdleTag>(),
                ComponentType.ReadOnly<HasDriverTag>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentEnabled<TrafficIdleTag>(entity, false);
        }
    }
}