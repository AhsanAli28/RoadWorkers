using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class CrossHairScaleSystem : BeginSimulationSystemBase
    {
        protected override void OnUpdate()
        {
            var crossHairUpdateScaleTagLookup = SystemAPI.GetComponentLookup<CrossHairUpdateScaleTag>(false);

            Entities
                    .WithoutBurst()
                    .WithAll<CrossHairUpdateScaleTag>()
                    .ForEach((
                Entity entity,
                ref CrossHairComponent crossHairComponent) =>
            {
                if (crossHairComponent.CurrentScale != crossHairComponent.TargetScale)
                {
                    crossHairComponent.CurrentScale = crossHairComponent.TargetScale;

                    var transform = EntityManager.GetComponentObject<Transform>(entity);

                    transform.localScale = new Vector3(crossHairComponent.CurrentScale, crossHairComponent.CurrentScale, crossHairComponent.CurrentScale);
                }

                crossHairUpdateScaleTagLookup.SetComponentEnabled(entity, false);
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
