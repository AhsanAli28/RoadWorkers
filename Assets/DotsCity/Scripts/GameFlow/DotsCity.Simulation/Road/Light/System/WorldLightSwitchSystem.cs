using Spirit604.DotsCity.Simulation.Level.Props;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateAfter(typeof(WorldLightListenerSystem))]
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [RequireMatchingQueriesForUpdate]
    public partial struct WorldLightSwitchSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchLightJob = new SwitchLightJob()
            {
                MaterialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>(false),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true),
            };

            switchLightJob.Schedule();
        }

        [WithNone(typeof(PropsDamagedTag))]
        [BurstCompile]
        private partial struct SwitchLightJob : IJobEntity
        {
            public ComponentLookup<MaterialMeshInfo> MaterialMeshInfoLookup;

            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            void Execute(
                ref LightFrameHandlerStateComponent lightFrameHandlerStateComponent,
                EnabledRefRW<LightHandlerStateUpdateTag> lightHandlerStateUpdateTagRW,
                in LightFrameHandlerEntityComponent lightHandlerComponent,
                in LightFrameData lightFrameData)
            {
                lightHandlerStateUpdateTagRW.ValueRW = false;

                var lightHandlerData = LightHandlerLookup[lightHandlerComponent.RelatedEntityHandler];

                var newState = lightHandlerData.State;
                var previousLightState = lightFrameHandlerStateComponent.CurrentLightState;

                lightFrameHandlerStateComponent.CurrentLightState = newState;

                SetLightEntityState(ref MaterialMeshInfoLookup, newState, previousLightState, in lightFrameData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetLightEntityState(ref ComponentLookup<MaterialMeshInfo> materialMeshInfoLookup, LightState newLightState, LightState previousLightState, in LightFrameData light)
        {
            switch (previousLightState)
            {
                case LightState.Uninitialized:
                    break;
                case LightState.RedYellow:
                    {
                        if (light.YellowEntity != Entity.Null)
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.YellowEntity, false);
                        }

                        if (light.RedEntity != Entity.Null)
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.RedEntity, false);
                        }

                        break;
                    }
                case LightState.Green:
                    {
                        if (light.GreenEntity != Entity.Null)
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.GreenEntity, false);
                        }

                        break;
                    }
                case LightState.Yellow:
                    {
                        if (light.YellowEntity != Entity.Null)
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.YellowEntity, false);
                        }

                        break;
                    }
                case LightState.Red:
                    {
                        if (light.RedEntity != Entity.Null)
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.RedEntity, false);
                        }

                        break;
                    }
            }

            if (light.YellowEntity != Entity.Null)
            {
                switch (newLightState)
                {
                    case LightState.Uninitialized:
                        break;
                    case LightState.RedYellow:
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.YellowEntity, true);
                            materialMeshInfoLookup.SetComponentEnabled(light.RedEntity, true);
                            break;
                        }
                    case LightState.Green:
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.GreenEntity, true);
                            break;
                        }
                    case LightState.Yellow:
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.YellowEntity, true);
                            break;
                        }
                    case LightState.Red:
                        {
                            materialMeshInfoLookup.SetComponentEnabled(light.RedEntity, true);
                            break;
                        }
                }
            }
            else
            {
                if (newLightState == LightState.Green)
                {
                    materialMeshInfoLookup.SetComponentEnabled(light.GreenEntity, true);
                }
                else
                {
                    materialMeshInfoLookup.SetComponentEnabled(light.RedEntity, true);
                }
            }
        }
    }
}