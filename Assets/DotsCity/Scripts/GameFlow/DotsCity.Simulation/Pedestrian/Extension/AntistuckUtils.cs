using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class AntistuckUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ActivateAntistuck(
            ref EntityCommandBuffer commandBuffer,
            Entity entity,
            in BlobAssetReference<AntistuckConfig> antistuckConfig,
            ref DestinationComponent destinationComponent)
        {
            if (antistuckConfig.Value.AntistuckEnabled)
            {
                ActivateAntistuck(ref commandBuffer, entity);
            }
            else
            {
                destinationComponent = destinationComponent.SwapBack();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ActivateAntistuck(
            ref EntityCommandBuffer commandBuffer,
            Entity entity)
        {
            commandBuffer.AddComponent<AntistuckActivateTag>(entity);
            commandBuffer.AddComponent<AntistuckDestinationComponent>(entity);
            commandBuffer.AddComponent<AntistuckDeactivateTag>(entity);
            commandBuffer.SetComponentEnabled<AntistuckDeactivateTag>(entity, false);
            commandBuffer.AddComponent<CustomMovementTag>(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentTypeSet GetAntistuckSet()
        {
            return new ComponentTypeSet(
                ComponentType.ReadOnly<AntistuckActivateTag>(),
                ComponentType.ReadOnly<AntistuckDestinationComponent>(),
                ComponentType.ReadOnly<AntistuckDeactivateTag>(),
                ComponentType.ReadOnly<CustomMovementTag>()
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAntistuck(
            ref EntityCommandBuffer commandBuffer,
            Entity entity)
        {
            commandBuffer.RemoveComponent<AntistuckActivateTag>(entity);
            commandBuffer.RemoveComponent<AntistuckDestinationComponent>(entity);
            commandBuffer.RemoveComponent<AntistuckDeactivateTag>(entity);
            commandBuffer.RemoveComponent<CustomMovementTag>(entity);
        }
    }
}
