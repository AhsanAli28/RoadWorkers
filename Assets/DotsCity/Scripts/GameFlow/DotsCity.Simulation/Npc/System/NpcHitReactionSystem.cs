using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Npc
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class NpcProcessHitReactionSystem : BeginSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithAll<ProcessHitReactionTag, AliveTag>()
            .WithAll<NpcTag>()
            .ForEach((
                Entity entity,
                Transform npc,
                ref HealthComponent healthComponent) =>
            {
                var npcHitReaction = npc.GetComponent<INpcHitReaction>();
                npcHitReaction?.HandleHitReaction(healthComponent.HitPosition, healthComponent.HitDirection);
                commandBuffer.SetComponentEnabled<ProcessHitReactionTag>(entity, false);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}