using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class ObstacleLayoutHelper
    {
        public static ObstacleLayout GetLayout(float3 position, quaternion rotation, float3 extents, float targetOffset = 0, bool calcLimits = true)
        {
            ObstacleLayout obstacleLayout;

            if (targetOffset == 0)
            {
                obstacleLayout = new ObstacleLayout(position, rotation, extents, calcLimits);
            }
            else
            {
                obstacleLayout = new ObstacleLayout(position, rotation, extents, targetOffset, calcLimits);
            }

            return obstacleLayout;
        }

        public static Bounds GetRotatedBound(float3 position, quaternion rotation, float3 extents, float targetOffset = 0)
        {
            var obs = new ObstacleLayout(position, rotation, extents);

            float3 currentExtents = obs.GetCurrentExtents();
            currentExtents += new float3(targetOffset, 0, targetOffset);

            var bounds = new Bounds()
            {
                center = position,
                extents = new float3(currentExtents.x, extents.y * 2, currentExtents.z)
            };

            return bounds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersects(this ref ObstacleLayout srcLayout, ObstacleLayout dstLayout, bool fullCalc = false)
        {
            var intersects =
                srcLayout.Limits.x <= dstLayout.Limits.y && srcLayout.Limits.y >= dstLayout.Limits.x &&
                srcLayout.Limits.z <= dstLayout.Limits.w && srcLayout.Limits.w >= dstLayout.Limits.z;

            if (!intersects)
                return false;

            if (!fullCalc)
                return true;

            var intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftTopPoint, srcLayout.RightTopPoint, dstLayout.LeftTopPoint, dstLayout.LeftBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftTopPoint, srcLayout.RightTopPoint, dstLayout.RightTopPoint, dstLayout.RightBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftBottomPoint, srcLayout.RightBottomPoint, dstLayout.LeftTopPoint, dstLayout.LeftBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftBottomPoint, srcLayout.RightBottomPoint, dstLayout.RightTopPoint, dstLayout.RightBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftTopPoint, srcLayout.LeftBottomPoint, dstLayout.LeftTopPoint, dstLayout.RightTopPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.RightTopPoint, srcLayout.RightBottomPoint, dstLayout.LeftTopPoint, dstLayout.RightTopPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.LeftTopPoint, srcLayout.LeftBottomPoint, dstLayout.LeftBottomPoint, dstLayout.RightBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(srcLayout.RightTopPoint, srcLayout.RightBottomPoint, dstLayout.LeftBottomPoint, dstLayout.RightBottomPoint);

            if (intersectPoint != Vector3.zero) return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetBoundEdges(
            Vector3 leftBottomPoint,
            Vector3 rightBottomPoint,
            Vector3 leftTopPoint,
            Vector3 rightTopPoint)
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            if (minX > leftBottomPoint.x)
            {
                minX = leftBottomPoint.x;
            }
            if (minX > rightBottomPoint.x)
            {
                minX = rightBottomPoint.x;
            }
            if (minX > rightTopPoint.x)
            {
                minX = rightTopPoint.x;
            }
            if (minX > leftTopPoint.x)
            {
                minX = leftTopPoint.x;
            }

            if (minZ > leftBottomPoint.z)
            {
                minZ = leftBottomPoint.z;
            }
            if (minZ > rightBottomPoint.z)
            {
                minZ = rightBottomPoint.z;
            }
            if (minZ > rightTopPoint.z)
            {
                minZ = rightTopPoint.z;
            }
            if (minZ > leftTopPoint.z)
            {
                minZ = leftTopPoint.z;
            }

            if (maxX < leftBottomPoint.x)
            {
                maxX = leftBottomPoint.x;
            }
            if (maxX < rightBottomPoint.x)
            {
                maxX = rightBottomPoint.x;
            }
            if (maxX < rightTopPoint.x)
            {
                maxX = rightTopPoint.x;
            }
            if (maxX < leftTopPoint.x)
            {
                maxX = leftTopPoint.x;
            }

            if (maxZ < leftBottomPoint.z)
            {
                maxZ = leftBottomPoint.z;
            }
            if (maxZ < rightBottomPoint.z)
            {
                maxZ = rightBottomPoint.z;
            }
            if (maxZ < rightTopPoint.z)
            {
                maxZ = rightTopPoint.z;
            }
            if (maxZ < leftTopPoint.z)
            {
                maxZ = leftTopPoint.z;
            }

            return new Vector4(minX, maxX, minZ, maxZ);
        }
    }
}