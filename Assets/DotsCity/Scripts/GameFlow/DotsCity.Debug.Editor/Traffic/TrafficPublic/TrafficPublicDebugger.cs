using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using System;
using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficPublicDebugger : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficDebug.html#traffic-public-debugger")]
        [SerializeField] private string link;

        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color textColor = Color.white;

#if UNITY_EDITOR

        private EntityManager entityManager;
        private EntityQuery trafficPublicesQuery;

        private TrafficPublicIdleDebugger trafficPublicIdleDebugger;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            trafficPublicesQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficPublicTag>());

            trafficPublicIdleDebugger = new TrafficPublicIdleDebugger(entityManager);
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug || !Application.isPlaying)
            {
                return;
            }

            var entities = trafficPublicesQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                trafficPublicIdleDebugger.Tick(entities[i], textColor);
            }

            entities.Dispose();
        }

#endif
    }

#if UNITY_EDITOR

    public class TrafficPublicIdleDebugger : TrafficDebugBase
    {
        private DefaultWorldTimeSystem defaultWorldTimeSystem;
        private StringBuilder sb = new StringBuilder();

        public TrafficPublicIdleDebugger(EntityManager entityManager) : base(entityManager)
        {
            defaultWorldTimeSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DefaultWorldTimeSystem>();
        }

        public override Color GetBoundsColor(Entity entity)
        {
            var color = EntityManager.HasComponent(entity, typeof(TrafficPublicIdleComponent)) ? Color.red : Color.green;
            return color;
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            bool isWagon = EntityManager.HasComponent(entity, typeof(TrafficWagonComponent));
            var ownerEntity = entity;

            if (isWagon)
            {
                ownerEntity = EntityManager.GetComponentData<TrafficWagonComponent>(entity).OwnerEntity;
            }

            sb.Clear();

            bool isIdle = false;

            if (EntityManager.HasComponent(ownerEntity, typeof(TrafficPublicIdleComponent)))
            {
                isIdle = true;

                if (!isWagon)
                {
                    var currentTime = defaultWorldTimeSystem.CurrentTime;
                    var deactivateTime = EntityManager.GetComponentData<TrafficPublicIdleComponent>(ownerEntity).DeactivateTime;
                    float remainTime = MathF.Round(deactivateTime - currentTime, 1);

                    sb.Append($"At bus stop. Time remain: ")
                        .Append($"{remainTime}")
                        .Append(Environment.NewLine);
                }
            }
            else
            {
                sb.Append($"On the way").Append(Environment.NewLine);
            }

            if (isIdle)
            {
                if (EntityManager.IsComponentEnabled<TrafficPublicProccessExitTag>(entity))
                {
                    sb.Append($"State: Exitting").Append(Environment.NewLine);
                }
                else if (EntityManager.IsComponentEnabled<TrafficPublicExitCompleteTag>(entity))
                {
                    sb.Append($"State: Entering").Append(Environment.NewLine);
                }

                var capacity = EntityManager.GetComponentData<CarCapacityComponent>(entity);
                sb.Append($"Capacity: {capacity.EnteredCount}/{capacity.MaxCapacity}").Append(Environment.NewLine);
            }

            return sb;
        }
    }

#endif
}
