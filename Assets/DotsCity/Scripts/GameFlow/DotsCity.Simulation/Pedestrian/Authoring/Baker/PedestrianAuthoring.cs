using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [TemporaryBakingType]
    public struct BakingTag : IComponentData { }

    [TemporaryBakingType]
    public struct PedestrianEntityEditorBakingTag : IComponentData { }

    [TemporaryBakingType]
    public struct CustomBakingTag : IComponentData { }

    public class PedestrianAuthoring : MonoBehaviour
    {
#if !UNITY_6000_1_OR_NEWER && UNITY_EDITOR
        private void Start() { } // For enable flag in inspector
#endif

        class PedestrianAuthoringBaker : Baker<PedestrianAuthoring>
        {
            public override void Bake(PedestrianAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(PedestrianMovementSettings));
                AddComponent(entity, typeof(PedestrianCommonSettings));
                AddComponent(entity, typeof(CollisionComponent));
                AddComponent(entity, typeof(StateComponent));
                AddComponent(entity, typeof(AnimationStateComponent));
                AddComponent(entity, typeof(NextStateComponent));
                AddComponent(entity, typeof(HealthComponent));
                AddComponent(entity, typeof(AliveTag));
                AddComponent(entity, typeof(CircleColliderComponent));
                AddComponent(entity, typeof(DestinationComponent));
                AddComponent(entity, typeof(DestinationDistanceComponent));
                AddComponent(entity, typeof(NavAgentComponent));
                AddComponent(entity, typeof(NavAgentSteeringComponent));
                AddComponent(entity, typeof(BakingTag));
                AddComponent(entity, typeof(CustomBakingTag));
                AddComponent(entity, typeof(PedestrianEntityEditorBakingTag));

                AddComponent(entity, CullComponentsExtension.GetComponentSet());

                // IEnableable event components
                AddComponent(entity, typeof(HasTargetTag));
                AddComponent(entity, typeof(ReachTargetTag));
                AddComponent(entity, typeof(ProcessEnterDefaultNodeTag));
                AddComponent(entity, typeof(MovementStateChangedEventTag));
                AddComponent(entity, typeof(AchievedNavTargetTag));
                AddComponent(entity, typeof(IdleTag));
                AddComponent(entity, typeof(HasCollisionTag));
                AddComponent(entity, typeof(HasSkinTag));
                AddComponent(entity, typeof(UpdateNavTargetTag));

#if PROJECTDAWN_NAV
                AddComponent(entity, typeof(AgentBakingTag));
#endif

                this.SetComponentEnabled<ReachTargetTag>(entity, false);
                this.SetComponentEnabled<ProcessEnterDefaultNodeTag>(entity, false);
                this.SetComponentEnabled<MovementStateChangedEventTag>(entity, false);
                this.SetComponentEnabled<AchievedNavTargetTag>(entity, false);
                this.SetComponentEnabled<IdleTag>(entity, false);
                this.SetComponentEnabled<HasCollisionTag>(entity, false);
                this.SetComponentEnabled<HasSkinTag>(entity, false);

                AddComponent(entity, new NpcTypeComponent { Type = NpcType.Pedestrian });

                PoolEntityUtils.AddPoolComponents(this, entity, EntityWorldType.PureEntity);
            }
        }
    }
}
