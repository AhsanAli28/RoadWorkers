using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficCarHybridMonoEntityAuthoring : TrafficCarEntityAuthoring
    {
        [Tooltip("Set the maximum steering angle according to the maximum steering angle in your custom controller")]
        [SerializeField] private float maxSteeringAngle = 40;
        [SerializeField] private bool interpolate = true;

        public float MaxSteeringAngle { get => maxSteeringAngle; set => maxSteeringAngle = value; }

        protected class TrafficCarHybridMonoEntityAuthoringBaker : Baker<TrafficCarHybridMonoEntityAuthoring>
        {
            public override void Bake(TrafficCarHybridMonoEntityAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                CarEntityAuthoringBase.CarEntityAuthoringBaseBaker.Bake(this, entity, authoring);
                TrafficCarEntityAuthoring.TrafficEntityAuthoringBaker.Bake(this, entity, authoring);

                AddComponent(entity, new CustomSteeringData(authoring.maxSteeringAngle));

                AddComponent(entity, new MonoAdapterComponent()
                {
                    Interpolate = authoring.interpolate
                });

                AddComponent<TrafficMonoMovementDisabled>(entity);
                SetComponentEnabled<TrafficMonoMovementDisabled>(entity, true);
            }
        }
    }
}