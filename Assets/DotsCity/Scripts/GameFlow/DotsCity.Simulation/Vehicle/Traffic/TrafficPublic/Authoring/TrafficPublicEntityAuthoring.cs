using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [TemporaryBakingType]
    public struct TrafficPublicEntityBakingTag : IComponentData { }

    [RequireComponent(typeof(CarCapacityAuthoring))]
    public class TrafficPublicEntityAuthoring : MonoBehaviour, ICustomTrafficCar
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficPublic.html#trafficpublicauthoring-component")]
        [SerializeField] private string link;

        [Tooltip("The vehicle will only be spawned on TrafficPublicRoute paths.")]
        [SerializeField] private bool predefinedRoad;

        [Tooltip("Min/max idle time at the public stop station")]
        [SerializeField][MinMaxSlider(0, 100)] private Vector2 minMaxIdleTime = new Vector2(5f, 10f);

        [Tooltip("The delay time after the transport has stopped and the beginning of the exit from it")]
        [SerializeField][Range(0, 10)] private float idleTimeAfterStop = 1f;

        [Tooltip("Min/max number of pedestrians that can exit the station at a time")]
        [SerializeField][MinMaxSlider(0, 100)] private Vector2Int minMaxPedestrianExitCount = new Vector2Int(1, 15);

        [Tooltip("Min/max delay between entrances to public transport.")]
        [SerializeField][MinMaxSlider(0, 10)] private Vector2 enterExitDelayDuration = new Vector2(0.7f, 1.2f);

        public bool PredefinedRoad { get => predefinedRoad; set => predefinedRoad = value; }

        public bool CustomHandling => predefinedRoad;

        class TrafficPublicEntityAuthoringBaker : Baker<TrafficPublicEntityAuthoring>
        {
            public override void Bake(TrafficPublicEntityAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent<TrafficPublicTag>(entity);

                if (authoring.predefinedRoad)
                {
                    AddComponent<TrafficFixedRouteTag>(entity);
                }

                AddComponent(entity, new TrafficPublicIdleSettingsComponent()
                {
                    MinIdleTime = authoring.minMaxIdleTime.x,
                    MaxIdleTime = authoring.minMaxIdleTime.y,
                    IdleTimeAfterStop = authoring.idleTimeAfterStop,
                });

                AddComponent(entity, new TrafficPublicExitSettingsComponent()
                {
                    MinPedestrianExitCount = authoring.minMaxPedestrianExitCount.x,
                    MaxPedestrianExitCount = authoring.minMaxPedestrianExitCount.y,
                    EnterExitDelayDuration = authoring.enterExitDelayDuration
                });

                AddComponent<TrafficPublicEntityBakingTag>(entity);

                AddComponent<TrafficPublicExitCompleteTag>(entity);
                AddComponent<TrafficPublicProccessExitTag>(entity);
            }
        }
    }
}