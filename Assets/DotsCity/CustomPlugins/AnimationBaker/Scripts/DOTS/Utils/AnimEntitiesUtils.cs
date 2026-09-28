using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public static class AnimEntitiesUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UpdateAnimation(
            ref SkinUpdateComponent skinUpdateComponent,
            ref EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
            int animationHash,
            bool uniqueMaterial)
        {
            if (UpdateAnimation(ref skinUpdateComponent, ref updateSkinTagRW, animationHash))
            {
                skinUpdateComponent.UniqueAnimation = uniqueMaterial;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool UpdateAnimation(
            ref SkinUpdateComponent skinUpdateComponent,
            ref EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
            int animationHash)
        {
            if (animationHash == -1)
            {
                return false;
            }

            skinUpdateComponent.NewAnimationHash = animationHash;
            updateSkinTagRW.ValueRW = true;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartAnimationTransition(
            in NativeHashMap<int, Entity> transitions,
            ref AnimationTransitionData animationTransitionData,
            ref EnabledRefRW<HasAnimTransitionTag> hasAnimTransitionTagRW,
            int transitionHash,
            float startTime)
        {
            if (transitions.TryGetValue(transitionHash, out Entity entryAnimEntity) && entryAnimEntity != Entity.Null)
            {
                animationTransitionData.CurrentAnimationState = entryAnimEntity;
                animationTransitionData.StartTime = startTime;
                hasAnimTransitionTagRW.ValueRW = true;
                return true;
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"AnimUtils.StartAnimationTransition. Transition not found. Start transition hash {transitionHash}");
#endif
            }

            return false;
        }
    }
}
