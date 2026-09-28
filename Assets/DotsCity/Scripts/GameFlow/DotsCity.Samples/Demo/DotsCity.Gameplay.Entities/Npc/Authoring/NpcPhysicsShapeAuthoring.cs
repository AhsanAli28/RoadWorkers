//using Spirit604.DotsCity.Common;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class NpcPhysicsShapeAuthoring : MonoBehaviour
    {
        [SerializeField][Range(0.01f, 1f)] private float circleColliderRadius = 0.5f;
        [SerializeField] private PhysicsCategoryTags groundCastLayer = new PhysicsCategoryTags() { Category00 = true };
        [SerializeField] private GroundCasterAuthoring groundCasterPoint;

        public class NpcPhysicsShapeAuthoringBaker : Baker<NpcPhysicsShapeAuthoring>
        {
            public override void Bake(NpcPhysicsShapeAuthoring authoring)
            {
                Bake(this, authoring);
            }

            public static void Bake(IBaker baker, NpcPhysicsShapeAuthoring authoring)
            {
                var entity = baker.GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                baker.AddComponent(entity, typeof(NpcTag));
                baker.AddComponent(entity, typeof(CollisionComponent));
                baker.AddComponent(entity, typeof(InputComponent));
                baker.AddComponent(entity, typeof(NpcCombatStateComponent));
                baker.AddComponent(entity, typeof(AnimatorMovementComponent));
                baker.AddComponent(entity, typeof(RagdollComponent));
                baker.AddComponent(entity, typeof(VelocityComponent));
                baker.AddComponent(entity, typeof(AliveTag));
                baker.AddComponent(entity, typeof(NpcTargetComponent));
                baker.AddComponent(entity, typeof(NpcNavAgentComponent));
                baker.AddComponent(entity, typeof(NavAgentSteeringComponent));
                baker.AddComponent(entity, typeof(InterpolateTransformData));

                baker.AddComponent(entity, typeof(AchievedNavTargetTag));
                baker.AddComponent(entity, typeof(UpdateNavTargetTag));
                baker.AddComponent(entity, typeof(PersistNavigationTag));
                baker.AddComponent(entity, typeof(PersistNavigationComponent));
                baker.AddComponent(entity, typeof(EnabledNavigationTag));
                baker.AddComponent(entity, typeof(NavAgentTag));

                baker.AddComponent(entity, new NpcStateComponent()
                {
                    IsGrounded = true
                });

                baker.AddComponent(entity, typeof(ProcessHitReactionTag));
                baker.SetComponentEnabled<ProcessHitReactionTag>(entity, false);

                if (authoring.groundCasterPoint != null)
                {
                    baker.AddComponent(entity, new GroundCasterBakingData()
                    {
                        CastingLayer = authoring.groundCastLayer.Value
                    });

                    baker.AddComponent(entity, new GroundCasterRef()
                    {
                        CasterEntity = baker.GetEntity(authoring.groundCasterPoint, TransformUsageFlags.Dynamic)
                    });
                }

                baker.AddComponent(entity, typeof(AnimatorStateComponent));
                baker.AddComponent(entity, typeof(AnimatorFallingState));
                baker.AddComponent(entity, typeof(NpcBakingTag));

                baker.AddComponent(entity, new NpcTypeComponent { Type = NpcType.Player });
                baker.AddComponent(entity, new HealthComponent(4));
                baker.AddComponent(entity, new CircleColliderComponent { Radius = authoring.circleColliderRadius });
                baker.AddComponent(entity, new FactionTypeComponent { Value = FactionType.All });

                PoolEntityUtils.AddPoolComponents(baker, entity, EntityWorldType.HybridEntity);
            }
        }
    }
}