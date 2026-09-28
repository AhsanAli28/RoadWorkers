using Spirit604.CityEditor.Road;
using Spirit604.Extensions;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public static class RSGeneratorConfigExtension
    {
        public static void ApplyPreCreationSplineSettings(this RSGeneratorConfig config, RoadSegmentCreator creator, string name)
        {
            if (config.GetData(name, out var data))
            {
                if (data.ForceOneway)
                {
                    creator.oneWay = true;
                    creator.shouldRevertDirection = data.ReverseDirection;
                }

                if (data.CustomLaneCount)
                {
                    creator.laneCount = data.LaneCount;
                }

                creator.dividerWidth = data.DividerWidth;
            }
        }

        public static void ApplyPostCreationSplineSettings(this RSGeneratorConfig config, RoadSegmentCreator creator)
        {
            if (config.StripOutNodes)
            {
                creator.StripNodes(config.MinStripAngle, config.MinStripDistance, false);
            }

            if (config.GenerateSpawnNodes)
            {
                creator.GeneratePathInnerSpawnNodes(config.MinNodeOffsetDistance);
            }
        }

        public static void ApplySettings(this RSGeneratorConfig config, RoadSegmentCreator creator, RSGenType rsGenType, string name = null)
        {
            if (rsGenType != RSGenType.CustomPrefab)
            {
                if (config.ShouldAddPedestrianNodes(name))
                {
                    creator.addPedestrianNodes = true;

                    if (rsGenType == RSGenType.SplineRoad)
                    {
                        creator.SetAllCrosswalkEnabled(false);
                        creator.addAlongLine = true;
                        creator.nodeSpacing = config.NodeSpacing;
                        creator.lineNodeOffset = config.LineNodeOffset;
                    }
                }
                else
                {
                    creator.addPedestrianNodes = false;
                }
            }

            if (rsGenType == RSGenType.Crossing)
            {
                creator.straightRoadPathSpeedLimit = config.StraightSpeedLimit;
                creator.turnRoadPathSpeedLimit = config.TurnSpeedLimit;

                if (config.CrossingLightType == RSGeneratorConfig.LightType.Custom)
                {
                    var container = config.SharedLightStateContainer;

                    var crossroad = creator.trafficLightCrossroad;

                    if (container && crossroad.SharedStateContainer != container)
                    {
                        crossroad.SharedStateContainer = container;
                        EditorSaver.SetObjectDirty(crossroad);
                    }
                }

#if UNITY_EDITOR
                creator.OnTurnSpeedLimitChanged();
#endif
            }

            if (rsGenType == RSGenType.SplineRoad)
            {
                if (config.GetSpeedLimit(name, out var speedLimit))
                {
                    creator.straightRoadPathSpeedLimit = speedLimit;
                }
                else
                {
                    creator.straightRoadPathSpeedLimit = config.SplineSpeedLimit;
                }
            }

            creator.crossWalkNodeShape = config.NodeShapeType;

            creator.crosswalkNodeWidth = config.CrosswalkWidth;
            creator.crosswalkNodeHeight = config.CrosswalHeight;

            if (config.NodeShapeType == Gameplay.Road.NodeShapeType.Rectangle)
            {
                creator.crosswalkNodeHeight = config.CrosswalHeight;
            }

            if (config.PedestrianPathWidthType == RSGeneratorConfig.WidthType.Custom)
            {
                creator.SetCustomPedestrianWidth(config.PedestrianPathWidth);
            }

            if (config.CrosswalkOffsetType1 == RSGeneratorConfig.CrosswalkOffsetType.Custom)
            {
                creator.customCrossWalkOffset = config.CrosswalkOffset;
            }

#if UNITY_EDITOR
            creator.OnStraightSpeedLimitChanged();
            creator.OnCrosswalkSettingsChanged();
#endif
        }
    }
}
