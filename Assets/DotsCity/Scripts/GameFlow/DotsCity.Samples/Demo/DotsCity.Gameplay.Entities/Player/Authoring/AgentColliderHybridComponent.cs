using Spirit604.DotsCity.Hybrid.Core;
using UnityEngine;

#if PROJECTDAWN_NAV
using Spirit604.DotsCity.Core;
using Unity.Entities;
#endif

namespace Spirit604.DotsCity.Gameplay.Player
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "AgentColliderHybridComponent")]
    public class AgentColliderHybridComponent : HybridComponentBase
#if PROJECTDAWN_NAV
        , IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
#endif
    {

#pragma warning disable 0414

        [SerializeField] private float radius = 0.5f;
        [SerializeField] private float height = 2f;

#pragma warning restore 0414

#if PROJECTDAWN_NAV
        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<ProjectDawn.Navigation.Agent>(),
                ComponentType.ReadOnly<ProjectDawn.Navigation.AgentBody>(),
                ComponentType.ReadOnly<ProjectDawn.Navigation.AgentCollider>(),
                ComponentType.ReadOnly<ProjectDawn.Navigation.AgentShape>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity, ProjectDawn.Navigation.Agent.Default);
            entityManager.SetComponentData(entity, ProjectDawn.Navigation.AgentBody.Default);

            entityManager.SetComponentData(entity, new ProjectDawn.Navigation.AgentCollider()
            {
                Layers = ProjectDawn.Navigation.NavigationLayers.Everything
            });

            entityManager.SetComponentData(entity, new ProjectDawn.Navigation.AgentShape()
            {
                Height = height,
                Radius = radius,
                Type = ProjectDawn.Navigation.ShapeType.Cylinder
            });
        }
#endif
    }
}
