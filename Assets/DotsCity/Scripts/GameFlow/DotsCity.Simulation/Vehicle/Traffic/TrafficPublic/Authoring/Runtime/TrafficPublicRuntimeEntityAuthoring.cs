using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [RequireComponent(typeof(CarCapacityRuntimeAuthoring), typeof(TrafficCarRuntimeAuthoring), typeof(HybridEntityRuntimeAuthoring))]
    public class TrafficPublicRuntimeEntityAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficPublic.html#trafficpublicauthoring-component")]
        [SerializeField] private string link;

        [Tooltip("Min/max idle time at the public stop station")]
        [SerializeField][MinMaxSlider(0, 100)] private Vector2 minMaxIdleTime = new Vector2(5f, 10f);

        [Tooltip("The delay time after the transport has stopped and the beginning of the exit from it")]
        [SerializeField][Range(0, 10)] private float idleTimeAfterStop = 1f;

        [Tooltip("Min/max number of pedestrians that can exit the station at a time")]
        [SerializeField][MinMaxSlider(0, 100)] private Vector2Int minMaxPedestrianExitCount = new Vector2Int(1, 15);

        [Tooltip("Min/max delay between entrances to public transport.")]
        [SerializeField][MinMaxSlider(0, 10)] private Vector2 enterExitDelayDuration = new Vector2(0.7f, 1.2f);

        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<TrafficPublicTag>(),
                ComponentType.ReadOnly<TrafficPublicIdleSettingsComponent>(),
                ComponentType.ReadOnly<TrafficPublicExitSettingsComponent>(),
                ComponentType.ReadOnly<TrafficPublicExitCompleteTag>(),
                ComponentType.ReadOnly<TrafficPublicProccessExitTag>(),
        };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity, new TrafficPublicIdleSettingsComponent()
            {
                MinIdleTime = minMaxIdleTime.x,
                MaxIdleTime = minMaxIdleTime.y,
                IdleTimeAfterStop = idleTimeAfterStop,
            });

            entityManager.SetComponentData(entity, new TrafficPublicExitSettingsComponent()
            {
                MinPedestrianExitCount = minMaxPedestrianExitCount.x,
                MaxPedestrianExitCount = minMaxPedestrianExitCount.y,
                EnterExitDelayDuration = enterExitDelayDuration
            });

            entityManager.SetComponentEnabled<TrafficPublicExitCompleteTag>(entity, false);
            entityManager.SetComponentEnabled<TrafficPublicProccessExitTag>(entity, false);
        }
    }
}