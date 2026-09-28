using Spirit604.AnimationBaker.Entities;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianInitUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(
            ref EntityCommandBuffer commandBuffer,
            Entity pedestrianEntity,
            in SpawnParams spawnParams,
            in BlobAssetReference<PedestrianSettings> pedestrianSettings,
            bool crosswalk = false,
            int groupSpawnIndex = 0)
        {
            var spawnPosition = spawnParams.RigidTransform.pos;

            commandBuffer.SetComponent(pedestrianEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnParams.RigidTransform.rot.value));

            var rnd = new Random(spawnParams.Seed);

            var walkingSpeed = pedestrianSettings.Value.GetRandomWalkingSpeed(rnd);
            var runningSpeed = pedestrianSettings.Value.GetRandomRunningSpeed(rnd);

            commandBuffer.SetComponent(pedestrianEntity, new PedestrianMovementSettings
            {
                WalkingValue = walkingSpeed,
                RunningValue = runningSpeed,
                RotationSpeed = pedestrianSettings.Value.RotationSpeed,
            });

            var pedestrianSkinIndex = spawnParams.SkinIndex;

            if (pedestrianSkinIndex == -1)
            {
                var seed = MathUtilMethods.ModifySeed(spawnParams.Seed, groupSpawnIndex + pedestrianEntity.Index);

                var random = new Random(seed);

                int maxIndex = pedestrianSettings.Value.MaxSkinIndex;

                pedestrianSkinIndex = random.NextInt(0, maxIndex);
            }

            commandBuffer.SetComponent(pedestrianEntity, new PedestrianCommonSettings
            {
                SkinIndex = pedestrianSkinIndex
            });

            if (pedestrianSettings.Value.Health > 0)
            {
                commandBuffer.SetComponent(pedestrianEntity, new HealthComponent(pedestrianSettings.Value.Health));
            }

            ActionState actionState = !crosswalk ? ActionState.MovingToNextTargetPoint : ActionState.CrossingTheRoad;

            commandBuffer.SetComponent(pedestrianEntity, new StateComponent
            {
                MovementState = MovementState.Walking,
                ActionState = actionState
            });

            commandBuffer.SetComponent(pedestrianEntity, new AnimationStateComponent
            {
                AnimationState = AnimationState.Walking
            });

            commandBuffer.SetComponent(pedestrianEntity, spawnParams.DestinationComponent);

            if (pedestrianSettings.Value.HasRig && pedestrianSettings.Value.HasGPUSkin)
            {
                commandBuffer.SetComponent(pedestrianEntity, new SkinAnimatorData
                {
                    SkinIndex = pedestrianSkinIndex
                });
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitTalkState(ref EntityCommandBuffer commandBuffer, Entity pedestrianEntity, double stopTalkingTime)
        {
            var stateComponent = new NextStateComponent(ActionState.Talking);

            commandBuffer.SetComponent(pedestrianEntity, stateComponent);

            commandBuffer.AddComponent(pedestrianEntity, new TalkComponent()
            {
                StopTalkingTime = stopTalkingTime
            });

            AnimatorStateExtension.AddCustomAnimatorState(ref commandBuffer, pedestrianEntity, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitTalkState(ref EntityCommandBuffer commandBuffer, Entity pedestrianEntity, double stopTalkingTime, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            InitTalkState(ref commandBuffer, pedestrianEntity, stopTalkingTime);
            commandBuffer.SetComponent(pedestrianEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DisableTalkState(ref EntityCommandBuffer commandBuffer, Entity pedestrianEntity, in StateComponent stateComponent, ref NextStateComponent nextStateComponent)
        {
            commandBuffer.RemoveComponent<TalkComponent>(pedestrianEntity);
            AnimatorStateExtension.RemoveCustomAnimator(ref commandBuffer, pedestrianEntity, true);

            if (stateComponent.IsActionState(ActionState.Talking))
            {
                nextStateComponent.TryToSetNextState(ActionState.MovingToNextTargetPoint);
                commandBuffer.SetComponentEnabled<HasTargetTag>(pedestrianEntity, true);
                return true;
            }

            return false;
        }
    }
}
