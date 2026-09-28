using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    [TemporaryBakingType]
    public struct TrafficPublicRouteTempBakingData : IComponentData
    {
        public NativeArray<TrafficPublicRouteNodeTempData> RouteNodes;
    }

    public struct TrafficPublicRouteNodeTempData
    {
        public Entity TrafficNodeScopeEntity;
        public int PathInstanceId;
        public int SourceLaneIndex;
        public bool InitRotation;
        public bool RightPathDirection;
    }

    public class TrafficPublicRouteBaker : Baker<TrafficPublicRoute>
    {
        public override void Bake(TrafficPublicRoute route)
        {
            for (int i = 0; i < route.Routes.Count; i++)
            {
                if (route.Routes[i] == null || route.Routes[i].SourceTrafficNode == null)
                {
                    UnityEngine.Debug.Log($"TrafficPublicRoute {route.name} has null path or node!");
                    return;
                }
            }

            var routeEntity = CreateAdditionalEntity(TransformUsageFlags.None);

            AddComponent(routeEntity, new TrafficPublicRouteSettings()
            {
                MaxVehicleCount = route.MaxVehicleCount,
                PreferredIntervalDistanceSQ = route.PreferredIntervalDistance * route.PreferredIntervalDistance,
                TrafficPublicType = route.TrafficPublicType,
                VehicleModel = route.VehicleModel,
                IgnoreCamera = route.IgnoreCamera,
            });

            AddComponent(routeEntity, new TrafficPublicRouteCapacityComponent()
            {
            });

            var trafficRouteNodes = AddBuffer<FixedRouteNodeElement>(routeEntity);

            NativeList<TrafficPublicRouteNodeTempData> trafficPublicRouteNodeTempDatas = new NativeList<TrafficPublicRouteNodeTempData>(Allocator.Temp);

            for (int i = 0; i < route.Routes.Count; i++)
            {
                var path = route.Routes[i];

                var relatedTrafficNodeEntity = GetEntity(path.SourceTrafficNode.gameObject, TransformUsageFlags.Dynamic);

                Vector3 nodePosition = Vector3.zero;
                Vector3 subNodeStartPosition = Vector3.zero;
                Quaternion nodeRotation = default;
                Quaternion endRotation = default;
                int isChangeLaneNode = 0;
                int localTargetWaypointIndex = -1;
                float customSpeedLimit = 0;
                bool initRotation = false;

                var nodeType = route.GetNodeType(path);

                switch (nodeType)
                {
                    case TrafficPublicRoute.NodeType.Default:
                        {
                            nodePosition = path.WayPoints[0].transform.position;
                            initRotation = true;
                            break;
                        }
                    case TrafficPublicRoute.NodeType.StartTransition:
                        {
                            nodePosition = path.WayPoints[0].transform.position;
                            subNodeStartPosition = route.GetSourceTransitionPoint(path, true);
                            var transition = route.GetTransition(path, true);

                            var targetPosition = route.GetSourceTransitionPoint(transition.TargetPath, false);
                            nodeRotation = path.SourceTrafficNode.transform.rotation;
                            endRotation = Quaternion.LookRotation(targetPosition - nodePosition);
                            isChangeLaneNode = 1;
                            customSpeedLimit = transition.SpeedLimit / ProjectConstants.KmhToMs_RATE;
                            break;
                        }
                    case TrafficPublicRoute.NodeType.EndTransition:
                        {
                            nodePosition = route.GetSourceTransitionPoint(path, false);
                            nodeRotation = path.SourceTrafficNode.transform.rotation;

                            localTargetWaypointIndex = path.GetTargetWaypointIndexByPoint(nodePosition);

                            endRotation = nodeRotation;
                            break;
                        }
                }

                bool rightPathDirection = path.SourceTrafficNode.HasPath(path);

                trafficPublicRouteNodeTempDatas.Add(new TrafficPublicRouteNodeTempData()
                {
                    TrafficNodeScopeEntity = relatedTrafficNodeEntity,
                    PathInstanceId = path.GetInstanceID(),
                    SourceLaneIndex = path.SourceLaneIndex,
                    RightPathDirection = rightPathDirection,
                    InitRotation = initRotation
                });

                int pathKey = -1;

                FixedRouteNodeElement routeElement = new FixedRouteNodeElement()
                {
                    TrafficNodeEntity = relatedTrafficNodeEntity,
                    Position = nodePosition,
                    Rotation = nodeRotation,
                    PathKey = pathKey,
                    CustomLocalTargetWaypointIndex = localTargetWaypointIndex
                };

                trafficRouteNodes.Add(routeElement);

                if (subNodeStartPosition != Vector3.zero)
                {
                    routeElement = new FixedRouteNodeElement()
                    {
                        TrafficNodeEntity = relatedTrafficNodeEntity,
                        Position = subNodeStartPosition,
                        Rotation = endRotation,
                        IsChangeLaneNode = isChangeLaneNode,
                        PathKey = pathKey,
                        CustomLocalTargetWaypointIndex = localTargetWaypointIndex,
                        CustomSpeedLimit = customSpeedLimit
                    };

                    trafficRouteNodes.Add(routeElement);

                    trafficPublicRouteNodeTempDatas.Add(new TrafficPublicRouteNodeTempData()
                    {
                        TrafficNodeScopeEntity = relatedTrafficNodeEntity,
                        PathInstanceId = path.GetInstanceID(),
                        SourceLaneIndex = path.SourceLaneIndex,
                        RightPathDirection = rightPathDirection,
                        InitRotation = initRotation
                    });
                }
            }

            TrafficPublicRouteTempBakingData trafficPublicRouteTempBakingData = new TrafficPublicRouteTempBakingData()
            {
                RouteNodes = new NativeArray<TrafficPublicRouteNodeTempData>(trafficPublicRouteNodeTempDatas.ToArray(Allocator.Temp), Allocator.Temp)
            };

            AddComponent(routeEntity, trafficPublicRouteTempBakingData);
        }
    }
}