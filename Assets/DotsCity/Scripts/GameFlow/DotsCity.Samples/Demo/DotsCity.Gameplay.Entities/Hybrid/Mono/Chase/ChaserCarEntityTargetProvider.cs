using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser
{
    public class ChaserCarEntityTargetProvider : ShootTargetProviderBase, IAIShotTargetProvider
    {
        private readonly EntityManager entityManager;
        private readonly EntityQuery playerQuery;
        private LocalToWorld lastTargetTranform;

        public ChaserCarEntityTargetProvider()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            playerQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<LocalToWorld>());
        }

        public override bool HasTarget => playerQuery.CalculateEntityCount() == 1;

        public Vector3 TargetForward => lastTargetTranform.Forward;

        public Vector3 TargetVelocity
        {
            get
            {
                if (HasTarget)
                {
                    var entity = playerQuery.GetSingletonEntity();

                    if (entityManager.HasComponent<VelocityComponent>(entity))
                    {
                        return entityManager.GetComponentData<VelocityComponent>(entity).Value;
                    }
                }

                return Vector3.zero;
            }
        }

        public override Vector3 GetTarget()
        {
            if (HasTarget)
            {
                var lastTargetTranform = playerQuery.GetSingleton<LocalToWorld>();
                return lastTargetTranform.Position;
            }

            return default;
        }
    }
}