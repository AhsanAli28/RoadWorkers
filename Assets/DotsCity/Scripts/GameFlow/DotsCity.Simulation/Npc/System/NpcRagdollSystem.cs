using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Npc
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class NpcRagdollSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<RagdollActivateEventTag>()
            .WithAny<NpcTag, CustomRagdollTag>()
            .ForEach((
                Entity entity,
                Transform npcTransform,
                ref RagdollComponent ragdollComponent) =>
            {
                if (ragdollComponent.Activated)
                    return;

                ragdollComponent.Activated = true;

                EntityManager.SetComponentEnabled<CopyTransformToGameObject>(entity, false);

                var npcHitReaction = npcTransform.GetComponent<INpcHitReaction>();

                if (npcHitReaction != null)
                {
                    npcHitReaction.ActivateDeathEffect(ragdollComponent.ForceDirection);
                }
                else
                {
#if UNITY_EDITOR
                    string skinText = string.Empty;

                    if (EntityManager.HasComponent<PedestrianCommonSettings>(entity))
                    {
                        var pedestrianCommonSettings = EntityManager.GetComponentData<PedestrianCommonSettings>(entity);
                        skinText = $"Pedestrian skinIndex '{pedestrianCommonSettings.SkinIndex}' . ";
                    }

                    UnityEngine.Debug.Log($"NpcRagdollSystem. {skinText}Hybrid entity doesn't have a component that implements the 'INpcHitReaction' interface.");
#endif
                }
            }).Run();
        }
    }
}