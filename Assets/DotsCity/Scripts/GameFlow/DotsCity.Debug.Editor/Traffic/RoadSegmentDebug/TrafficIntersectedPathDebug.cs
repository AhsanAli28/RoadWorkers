using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficIntersectedPathDebug : TrafficObstacleDistanceDebug
    {
        private const float POINT_DEBUG_RADIUS = 1f;
        private const string PRIORITY_TEXT = "Priority: ";

        public TrafficIntersectedPathDebug(EntityManager entityManager) : base(entityManager)
        {
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var priority = EntityManager.GetComponentData<TrafficPathComponent>(entity).Priority;

            StringBuilder sb = new StringBuilder();

            sb.Append(PRIORITY_TEXT);
            sb.Append(priority);

            return sb;
        }

        public override void Tick(Entity entity, Color fontColor)
        {
            base.Tick(entity, fontColor);

            var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;
            var bounds = EntityManager.GetComponentData<BoundsComponent>(entity);

            var graph = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>()).GetSingleton<PathGraphSystem.Singleton>();
            var trafficObstacleConfig = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficObstacleConfigReference>()).GetSingleton<TrafficObstacleConfigReference>();

            int pathIndex = EntityManager.GetComponentData<TrafficPathComponent>(entity).CurrentGlobalPathIndex;

            var intersectPathInfoList = graph.GetIntersectedPaths(pathIndex);

            if (intersectPathInfoList.Length > 0)
            {
                var currentPathEndPosition = graph.GetEndPosition(pathIndex);

                for (int i = 0; i < intersectPathInfoList.Length; i++)
                {
                    var intersectPathInfo = intersectPathInfoList[i];

                    float currentCarDistanceToIntersectPoint = math.distance(position, intersectPathInfo.IntersectPosition);

                    bool currentCarIsCloseEnoughToIntersectPointForCalculate = currentCarDistanceToIntersectPoint < trafficObstacleConfig.Config.Value.CalculateDistanceToIntersectPoint;
                    bool currentCarNearIntersectPoint = currentCarDistanceToIntersectPoint < bounds.Extents.z + trafficObstacleConfig.Config.Value.SizeOffsetToIntersectPoint;

                    Color color = Color.white;

                    if (!currentCarNearIntersectPoint)
                    {
                        color = currentCarIsCloseEnoughToIntersectPointForCalculate ? Color.green : Color.red;
                    }

                    Handles.color = color;
                    Handles.DrawWireDisc(intersectPathInfo.IntersectPosition, Vector3.up, POINT_DEBUG_RADIUS);

                    float roundedDistance = (float)System.Math.Round((double)currentCarDistanceToIntersectPoint, 2);

#if UNITY_EDITOR
                    EditorExtension.DrawWorldString(roundedDistance.ToString(), intersectPathInfo.IntersectPosition);
#endif
                }
            }
        }
    }
}