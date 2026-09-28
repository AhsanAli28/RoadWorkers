using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public static class AnimatorStateExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddCustomAnimatorState(ref EntityCommandBuffer commandBuffer, Entity entity, ref AnimationStateComponent animationStateComponent, AnimationState newAnimationState, bool customMovement = false)
        {
            AddCustomAnimatorState(ref commandBuffer, entity, customMovement);

            animationStateComponent.NewAnimationState = newAnimationState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddCustomAnimatorState(ref EntityCommandBuffer commandBuffer, Entity entity, AnimationState newAnimationState, bool customMovement = false)
        {
            AddCustomAnimatorState(ref commandBuffer, entity, customMovement);

            commandBuffer.SetComponent(entity, new AnimationStateComponent()
            {
                NewAnimationState = newAnimationState
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddCustomAnimatorState(ref EntityCommandBuffer commandBuffer, Entity entity, bool customMovement = false)
        {
            if (customMovement)
            {
                commandBuffer.AddComponent<CustomMovementTag>(entity);
            }

            commandBuffer.SetComponentEnabled<CustomAnimatorStateTag>(entity, true);
            commandBuffer.SetComponentEnabled<HasCustomAnimationTag>(entity, true);
            commandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ChangeAnimatorState(ref EntityCommandBuffer commandBuffer, Entity entity, ref AnimationStateComponent animationStateComponent, AnimationState newAnimationState, bool immediateUpdate = true)
        {
            animationStateComponent.NewAnimationState = newAnimationState;

            if (immediateUpdate)
            {
                commandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveCustomAnimator(ref EntityCommandBuffer commandBuffer, Entity entity, bool customMovement = false)
        {
            if (customMovement)
            {
                commandBuffer.RemoveComponent<CustomMovementTag>(entity);
            }

            commandBuffer.SetComponentEnabled<CustomAnimatorStateTag>(entity, false);
            commandBuffer.SetComponentEnabled<HasCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<UpdateCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<WaitForCustomAnimationTag>(entity, false);
            commandBuffer.SetComponentEnabled<ExitCustomAnimationTag>(entity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset(ref this AnimationStateComponent animationStateComponent)
        {
            animationStateComponent.NewAnimationState = default;
            animationStateComponent.NewStartPlaybacktime = 0;
        }
    }
}
