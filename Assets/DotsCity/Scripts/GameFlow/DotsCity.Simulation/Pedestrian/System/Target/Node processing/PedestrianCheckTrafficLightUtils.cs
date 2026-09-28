using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class PedestrianCheckTrafficLightUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Process(
            in ComponentLookup<LightHandlerComponent> lightHandlerLookup,
            in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsLookup,
            ref DestinationComponent destinationComponent,
            ref NextStateComponent nextStateComponent)
        {
            Entity sourceLightEntity = Entity.Null;
            Entity targetLightEntity = Entity.Null;
            NodeLightSettingsComponent sourceNodeLightSettingsComponent = default;

            bool isCrosswalk = false;

            if (nodeLightSettingsLookup.HasComponent(destinationComponent.PreviuosDestinationNode))
            {
                sourceNodeLightSettingsComponent = nodeLightSettingsLookup[destinationComponent.PreviuosDestinationNode];
                var sourceNodeLightSettings = sourceNodeLightSettingsComponent;
                sourceLightEntity = sourceNodeLightSettings.LightEntity;
            }

            if (nodeLightSettingsLookup.HasComponent(destinationComponent.DestinationNode))
            {
                var targetNodeLightSettings = nodeLightSettingsLookup[destinationComponent.DestinationNode];
                targetLightEntity = targetNodeLightSettings.LightEntity;
                isCrosswalk = sourceNodeLightSettingsComponent.IsCrosswalk(targetNodeLightSettings);
            }

            destinationComponent.PreviousLightEntity = sourceLightEntity;
            destinationComponent.DestinationLightEntity = targetLightEntity;

            LightState lightState = LightState.Uninitialized;

            if (lightHandlerLookup.HasComponent(sourceLightEntity))
            {
                lightState = lightHandlerLookup[sourceLightEntity].State;
            }

            if (isCrosswalk && lightState != LightState.Uninitialized)
            {
                if (lightHandlerLookup[sourceLightEntity].State == LightState.Green)
                {
                    nextStateComponent.TryToSetNextState(ActionState.CrossingTheRoad, ref destinationComponent);
                }
                else
                {
                    nextStateComponent.TryToSetNextState(ActionState.WaitForGreenLight, ref destinationComponent);
                }
            }
            else
            {
                if (isCrosswalk)
                {
                    nextStateComponent.TryToSetNextState(ActionState.CrossingTheRoad, ref destinationComponent);
                }
                else
                {
                    nextStateComponent.TryToSetNextState(ActionState.MovingToNextTargetPoint, ref destinationComponent);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ActionState GetCrossingState(in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsLookup, in DestinationComponent destinationComponent)
        {
            bool sameCrossroad = false;

            if (nodeLightSettingsLookup.HasComponent(destinationComponent.DestinationNode) && nodeLightSettingsLookup.HasComponent(destinationComponent.PreviuosDestinationNode))
            {
                var nodeSettings1 = nodeLightSettingsLookup[destinationComponent.DestinationNode];
                var nodeSettings2 = nodeLightSettingsLookup[destinationComponent.PreviuosDestinationNode];

                if (nodeSettings1.CrosswalkIndex == nodeSettings2.CrosswalkIndex && nodeSettings1.CrosswalkIndex != -1)
                {
                    sameCrossroad = true;
                }
            }

            if (sameCrossroad)
            {
                return ActionState.CrossingTheRoad;
            }
            else
            {
                return ActionState.MovingToNextTargetPoint;
            }
        }
    }
}