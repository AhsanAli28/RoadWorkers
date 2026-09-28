using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [TemporaryBakingType]
    public struct TrafficEntityBakingTag : IComponentData { }

    [TemporaryBakingType]
    public struct TrafficEntityEditorBakingTag : IComponentData { }

    public class TrafficCarEntityAuthoring : CarEntityAuthoringBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCar.html#trafficcarentityauthoring", 140)]
        [SerializeField] private string link;

        [EnumPopup]
        [SerializeField] private TrafficGroupType trafficGroup = TrafficGroupType.Default;

        public TrafficGroupType TrafficGroup { get => trafficGroup; set => trafficGroup = value; }

        protected class TrafficEntityAuthoringBaker : Baker<TrafficCarEntityAuthoring>
        {
            public override void Bake(TrafficCarEntityAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                CarEntityAuthoringBase.CarEntityAuthoringBaseBaker.Bake(this, entity, authoring);
                Bake(this, entity, authoring);
            }

            public static void Bake(IBaker baker, Entity entity, TrafficCarEntityAuthoring authoring)
            {
                baker.AddComponent(entity, typeof(TrafficTag));
                baker.AddComponent(entity, typeof(TrafficTargetDirectionComponent));
                baker.AddComponent(entity, typeof(TrafficSettingsComponent));
                baker.AddComponent(entity, typeof(TrafficPathComponent));
                baker.AddComponent(entity, typeof(TrafficStateComponent));
                baker.AddComponent(entity, typeof(TrafficLightDataComponent));
                baker.AddComponent(entity, typeof(VehicleInputReader));
                baker.AddComponent(entity, typeof(TrafficObstacleComponent));
                baker.AddComponent(entity, typeof(TrafficMovementComponent));
                baker.AddComponent(entity, typeof(TrafficDestinationComponent));
                baker.AddComponent(entity, typeof(TrafficApproachDataComponent));
                baker.AddComponent(entity, typeof(SpeedComponent));
                baker.AddComponent(entity, typeof(TrafficRotationSpeedComponent));

                baker.AddSharedComponent(entity, new WorldEntitySharedType(EntityWorldType.PureEntity));

                // TemporaryBakingType
                baker.AddComponent(entity, typeof(TrafficEntityBakingTag));
                baker.AddComponent(entity, typeof(TrafficEntityEditorBakingTag));

                // IEnableableComponent components
                baker.AddComponent(entity, typeof(TrafficSwitchTargetNodeRequestTag));
                baker.AddComponent(entity, typeof(TrafficAchievedTag));
                baker.AddComponent(entity, typeof(TrafficNextTrafficNodeRequestTag));
                baker.AddComponent(entity, typeof(TrafficEnteredTriggerNodeTag));
                baker.AddComponent(entity, typeof(TrafficEnteringTriggerNodeTag));
                baker.AddComponent(entity, typeof(TrafficInitTag));
                baker.AddComponent(entity, typeof(TrafficIdleTag));
                baker.AddComponent(entity, typeof(TrafficBackwardMovementTag));
                baker.AddComponent(entity, typeof(TrafficCustomMovementTag));

                baker.SetComponentEnabled<TrafficInitTag>(entity, false);
                baker.SetComponentEnabled<TrafficAchievedTag>(entity, false);
                baker.SetComponentEnabled<TrafficBackwardMovementTag>(entity, false);
                baker.SetComponentEnabled<TrafficCustomMovementTag>(entity, false);

                baker.AddComponent(entity, new TrafficTypeComponent { TrafficGroup = authoring.trafficGroup });

                var customTrafficCar = authoring.GetComponent<ICustomTrafficCar>();
                var isCustomCar = customTrafficCar != null;

                if (!isCustomCar || (isCustomCar && !customTrafficCar.CustomHandling))
                {
                    baker.AddComponent(entity, typeof(TrafficDefaultTag));
                }
            }
        }
    }
}