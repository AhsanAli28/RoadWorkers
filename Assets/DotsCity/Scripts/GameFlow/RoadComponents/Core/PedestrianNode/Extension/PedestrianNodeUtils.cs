using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spirit604.Gameplay.Road
{
    public static class PedestrianNodeUtils
    {
        private const float OVERLAP_RADIUS = 0.6f;

        public static PedestrianNode TryToFindConnectedObjects(PedestrianNode sourcePedestrianNode, bool allowConnectTrafficNode = true, Scene customRaycastScene = default)
        {
            if (sourcePedestrianNode == null)
            {
                return null;
            }

            PedestrianNode newNode = null;

#if UNITY_EDITOR
            var colliders = GetColliders(Event.current.mousePosition, out var spawnPosition, allowConnectTrafficNode, customRaycastScene);

            if (colliders?.Length > 0)
            {
                List<Transform> gos = colliders.Where(b => b.transform != sourcePedestrianNode.transform).Select(a => a.transform).ToList();

                if (gos?.Count > 0)
                {
                    var closestObject = VectorExtensions.FindClosestTarget(gos, spawnPosition);

                    if (closestObject != null)
                    {
                        var newSelectedNode = closestObject.GetComponent<PedestrianNode>();

                        if (newSelectedNode != null && newSelectedNode != sourcePedestrianNode)
                        {
                            newNode = newSelectedNode;
                        }

                        if (allowConnectTrafficNode)
                        {
                            TrafficNode trafficNode = closestObject.transform.GetComponent<TrafficNode>();

                            if (trafficNode != null)
                            {
                                sourcePedestrianNode.ConnectedTrafficNode = sourcePedestrianNode.ConnectedTrafficNode == trafficNode ? null : trafficNode;
                            }
                        }

                        EditorSaver.SetObjectDirty(sourcePedestrianNode);
                    }
                }
            }
#endif

            return newNode;
        }

        public static RaycastHit[] GetColliders(Vector2 mousePosition, out Vector3 hitPoint, bool includeTrafficNode = false, Scene customRaycastScene = default)
        {
            hitPoint = mousePosition.GUIScreenToWorldSpace();

            Ray ray = default;

#if UNITY_EDITOR
            ray = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePosition);
#endif

            if (!customRaycastScene.isLoaded)
            {
                if (Physics.Raycast(ray, out var hit, float.MaxValue, ~0, QueryTriggerInteraction.Ignore))
                {
                    hitPoint = hit.point;
                }
            }

            var castLayer = 1 << LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME);

            if (includeTrafficNode)
            {
                castLayer |= 1 << LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME);
            }

            if (!customRaycastScene.isLoaded)
            {
                var colliders = Physics.CapsuleCastAll(ray.origin, ray.origin + ray.direction * 1000, OVERLAP_RADIUS, ray.direction, float.MaxValue, castLayer);

                return colliders;
            }
            else
            {
                if (customRaycastScene.GetPhysicsScene().Raycast(ray.origin, ray.direction, out var hit, float.MaxValue, ~0, QueryTriggerInteraction.Ignore))
                {
                    return new RaycastHit[] { hit };
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public static PedestrianNode CreatePrefab(PedestrianNode sourceNode, Vector3 spawnPosition, Transform newParent = null)
        {
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(sourceNode.gameObject);

            PedestrianNode newNode = (UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject).GetComponent<PedestrianNode>();

            newNode.transform.position = spawnPosition;

            var currentParent = newParent ? newParent : sourceNode.transform.parent;
            newNode.transform.SetParent(currentParent);

            return newNode;
        }

        #region Gizmos

        public const float DefaultThickness = 0.4f;
        public const float DefaultYOffset = 0.2f;

        private static Vector3[] points = new Vector3[4];
        private static float[] angles = new float[4];

        public static void DrawNodeRouteConnection(PedestrianNode sourcePedestrianNode, PedestrianNode targetPedestrianNode, Color color, float yOffset = DefaultYOffset, float thickness = DefaultThickness)
        {
            DrawNodeRouteConnection(sourcePedestrianNode, targetPedestrianNode, sourcePedestrianNode.MaxPathWidth, targetPedestrianNode.MaxPathWidth, color, yOffset, thickness);
        }

        public static void DrawNodeRouteConnection(PedestrianNode sourcePedestrianNode, PedestrianNode targetPedestrianNode, float sourceWidth, float targetWidth, Color color, float yOffset = DefaultYOffset, float thickness = DefaultThickness)
        {
            DrawNodeRouteConnection(sourcePedestrianNode.transform.position, targetPedestrianNode.transform.position, Quaternion.identity, Quaternion.identity, sourceWidth, 0, targetWidth, 0, NodeShapeType.Circle, NodeShapeType.Circle, color, sourcePedestrianNode, targetPedestrianNode, yOffset, thickness);
        }

        public static void DrawNodeRouteConnection(Vector3 sourceNode, Vector3 targetNode, float sourceWidth, float targetWidth, Color color, float yOffset = DefaultYOffset, float thickness = DefaultThickness)
        {
            DrawNodeRouteConnection(sourceNode, targetNode, Quaternion.identity, Quaternion.identity, sourceWidth, 0, targetWidth, 0, NodeShapeType.Circle, NodeShapeType.Circle, color, null, null, yOffset, thickness);
        }

        public static void DrawNodeRouteConnection(
            Vector3 sourceNode,
            Vector3 targetNode,
            Quaternion sourceRotation,
            Quaternion targetRotation,
            float sourceWidth,
            float sourceHeight,
            float targetWidth,
            float targetHeight,
            NodeShapeType sourcePedestrianAreaShapeType,
            NodeShapeType targetPedestrianAreaShapeType,
            Color color,
            PedestrianNode sourcePedestrianNode = null,
            PedestrianNode targetPedestrianNode = null,
            float yOffset = DefaultYOffset,
            float thickness = DefaultThickness)
        {
            Vector3 tempOffset = new Vector3(0, yOffset);

            Vector3 sourcePoint1;
            Vector3 sourcePoint2;

            if (sourcePedestrianAreaShapeType == NodeShapeType.Circle)
            {
                sourcePoint1 = VectorExtensions.GetParallelPointAtSourceRelativeTargetLine(sourceNode, targetNode, sourceWidth) + tempOffset;
                sourcePoint2 = VectorExtensions.GetParallelPointAtSourceRelativeTargetLine(sourceNode, targetNode, -sourceWidth) + tempOffset;
            }
            else
            {
                GetEdgeRectPoints(targetNode, sourceNode, sourceRotation, sourceWidth, sourceHeight, out sourcePoint1, out sourcePoint2, sourcePedestrianNode, yOffset);
            }

            Vector3 targetPoint2;
            Vector3 targetPoint1;

            if (targetPedestrianAreaShapeType == NodeShapeType.Circle)
            {
                targetPoint2 = VectorExtensions.GetParallelPointAtSourceRelativeTargetLine(targetNode, sourceNode, targetWidth) + tempOffset;
                targetPoint1 = VectorExtensions.GetParallelPointAtSourceRelativeTargetLine(targetNode, sourceNode, -targetWidth) + tempOffset;
            }
            else
            {
                GetEdgeRectPoints(sourceNode, targetNode, targetRotation, targetWidth, targetHeight, out targetPoint2, out targetPoint1, targetPedestrianNode, yOffset);
            }

            GizmosDrawer.DrawLine(sourcePoint1, targetPoint1, color);
            GizmosDrawer.DrawLine(sourcePoint2, targetPoint2, color);
        }

        public static void GetRectanglePoints(PedestrianNode pedestrianNode, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4, float yOffset = 0)
        {
            var shouldCache = !pedestrianNode.CachedRect || !pedestrianNode.transform.position.IsEqual(pedestrianNode.CachedPosition) || !pedestrianNode.transform.rotation.eulerAngles.IsEqual(pedestrianNode.CachedRotation) || !pedestrianNode.CachedWidth.IsEqual(pedestrianNode.MaxPathWidth) || !pedestrianNode.CachedHeight.IsEqual(pedestrianNode.Height);

            Vector3 cachedP1;
            Vector3 cachedP2;
            Vector3 cachedP3;
            Vector3 cachedP4;

            if (shouldCache)
            {
                pedestrianNode.CachedRect = true;
                pedestrianNode.CachedPosition = pedestrianNode.transform.position;
                pedestrianNode.CachedRotation = pedestrianNode.transform.rotation.eulerAngles;
                pedestrianNode.CachedWidth = pedestrianNode.MaxPathWidth;
                pedestrianNode.CachedHeight = pedestrianNode.Height;

                CalculateRectanglePoints(pedestrianNode, out cachedP1, out cachedP2, out cachedP3, out cachedP4, 0);

                pedestrianNode.CachedP1 = cachedP1;
                pedestrianNode.CachedP2 = cachedP2;
                pedestrianNode.CachedP3 = cachedP3;
                pedestrianNode.CachedP4 = cachedP4;
            }
            else
            {
                cachedP1 = pedestrianNode.CachedP1;
                cachedP2 = pedestrianNode.CachedP2;
                cachedP3 = pedestrianNode.CachedP3;
                cachedP4 = pedestrianNode.CachedP4;
            }

            p1 = cachedP1;
            p2 = cachedP2;
            p3 = cachedP3;
            p4 = cachedP4;

            if (yOffset != 0)
            {
                var offset = new Vector3(0, yOffset);

                p1 += offset;
                p2 += offset;
                p3 += offset;
                p4 += offset;
            }
        }

        public static void GetEdgeRectPoints(Vector3 sourcePoint, Vector3 targetPoint, Quaternion targetRotation, float targetWidth, float targetHeight, out Vector3 point1, out Vector3 point2, PedestrianNode targetNode = null, float yOffset = 0.5f)
        {
            Vector3 p1, p2, p3, p4;

            if (targetNode == null)
            {
                CalculateRectanglePoints(targetPoint, targetRotation, targetWidth, targetHeight, out p1, out p2, out p3, out p4);
            }
            else
            {
                GetRectanglePoints(targetNode, out p1, out p2, out p3, out p4);
            }

            points[0] = p1;
            points[1] = p2;
            points[2] = p3;
            points[3] = p4;

            var dir = (targetPoint - sourcePoint).normalized;
            var dir1 = (p1 - sourcePoint).normalized;
            var dir2 = (p2 - sourcePoint).normalized;
            var dir3 = (p3 - sourcePoint).normalized;
            var dir4 = (p4 - sourcePoint).normalized;

            angles[0] = Vector3.SignedAngle(dir, dir1, Vector3.up);
            angles[1] = Vector3.SignedAngle(dir, dir2, Vector3.up);
            angles[2] = Vector3.SignedAngle(dir, dir3, Vector3.up);
            angles[3] = Vector3.SignedAngle(dir, dir4, Vector3.up);

            var maxAngle = float.MinValue;
            var minAngle = float.MaxValue;

            point1 = default;
            point2 = default;

            for (int i = 0; i < angles.Length; i++)
            {
                if (minAngle > angles[i])
                {
                    minAngle = angles[i];
                    point1 = points[i];
                }

                if (maxAngle < angles[i])
                {
                    maxAngle = angles[i];
                    point2 = points[i];
                }
            }

            var offset = new Vector3(0, yOffset);

            point1 += offset;
            point2 += offset;
        }

        public static void CalculateRectanglePoints(PedestrianNode pedestrianNode, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4, float yOffset = 0)
        {
            CalculateRectanglePoints(pedestrianNode.transform.position, pedestrianNode.transform.rotation, pedestrianNode.MaxPathWidth, pedestrianNode.Height, out p1, out p2, out p3, out p4, yOffset);
        }

        public static void CalculateRectanglePoints(Vector3 position, Quaternion rotation, float width, float height, out Vector3 p1, out Vector3 p2, out Vector3 p3, out Vector3 p4, float yOffset = 0)
        {
            float x1 = position.x - width / 2;
            float x2 = position.x + width / 2;
            float z1 = position.z - height / 2;
            float z2 = position.z + height / 2;

            p1 = new Vector3(x1, yOffset, z1);
            p2 = new Vector3(x2, yOffset, z1);
            p3 = new Vector3(x2, yOffset, z2);
            p4 = new Vector3(x1, yOffset, z2);

            p1 = RotatePoint(position, p1, rotation) + new Vector3(0, position.y);
            p2 = RotatePoint(position, p2, rotation) + new Vector3(0, position.y);
            p3 = RotatePoint(position, p3, rotation) + new Vector3(0, position.y);
            p4 = RotatePoint(position, p4, rotation) + new Vector3(0, position.y);
        }

        private static Vector3 RotatePoint(Vector3 center, Vector3 point, Quaternion rot)
        {
            var v = point - center; // The relative vector from P2 to P1
            v = rot * v; // Rotate
            v = v + center;

            return v;
        }

        #endregion
#endif
    }
}