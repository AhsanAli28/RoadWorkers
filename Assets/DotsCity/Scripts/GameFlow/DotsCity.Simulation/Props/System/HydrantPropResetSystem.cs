using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    [UpdateInGroup(typeof(DestroyGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class HydrantPropResetSystem : BeginInitSystemBase
    {
        protected override void OnUpdate()
        {
            var commandBuffer1 = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithAll<HydrantTag, PropsCustomResetTag>()
            .ForEach((
                Entity entity,
                ref PropsVFXData propsVFXData) =>
            {
                if (propsVFXData.RelatedEntity != Entity.Null)
                {
                    var particleSystem = EntityManager.GetComponentObject<ParticleSystem>(propsVFXData.RelatedEntity);
                    particleSystem.Stop();
                    particleSystem.gameObject.ReturnToPool();

                    commandBuffer1.DestroyEntity(propsVFXData.RelatedEntity);
                    propsVFXData.RelatedEntity = Entity.Null;
                }

                commandBuffer1.SetComponentEnabled<PropsCustomResetTag>(entity, false);
                commandBuffer1.SetComponentEnabled<PropsDamagedTag>(entity, false);

            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
