using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct ObstacleLayout
    {
        public Vector3 LeftBottomPoint { get; private set; }
        public Vector3 LeftTopPoint { get; private set; }
        public Vector3 RightBottomPoint { get; private set; }
        public Vector3 RightTopPoint { get; private set; }

        /// <summary>
        /// Bounds limit X - minX, Y - maxX, Z - minZ, W - maxZ
        /// </summary>
        public Vector4 Limits { get; private set; }

        public ObstacleLayout(Vector3 position, Quaternion rotation, Vector3 extents, bool calcLimits = false)
        {
            LeftBottomPoint = position + rotation * new Vector3(-extents.x, 0, -extents.z);
            LeftTopPoint = position + rotation * new Vector3(-extents.x, 0, extents.z);
            RightBottomPoint = position + rotation * new Vector3(extents.x, 0, -extents.z);
            RightTopPoint = position + rotation * new Vector3(extents.x, 0, extents.z);

            if (!calcLimits)
            {
                Limits = default;
            }
            else
            {
                Limits = ObstacleLayoutHelper.GetBoundEdges(LeftBottomPoint, RightBottomPoint, LeftTopPoint, RightTopPoint);
            }
        }

        public ObstacleLayout(Vector3 position, Quaternion rotation, Vector3 extents, float offset, bool calcLimits = false)
        {
            LeftBottomPoint = position + rotation * new Vector3(-extents.x - offset, 0, -extents.z - offset);
            LeftTopPoint = position + rotation * new Vector3(-extents.x - offset, 0, extents.z + offset);
            RightBottomPoint = position + rotation * new Vector3(extents.x + offset, 0, -extents.z - offset);
            RightTopPoint = position + rotation * new Vector3(extents.x + offset, 0, extents.z + offset);

            if (!calcLimits)
            {
                Limits = default;
            }
            else
            {
                Limits = ObstacleLayoutHelper.GetBoundEdges(LeftBottomPoint, RightBottomPoint, LeftTopPoint, RightTopPoint);
            }
        }

        public Vector3 GetCurrentExtents()
        {
            var edges = ObstacleLayoutHelper.GetBoundEdges(LeftBottomPoint, RightBottomPoint, LeftTopPoint, RightTopPoint);

            return new Vector3(edges.y - edges.x, 0, edges.w - edges.z) / 2;
        }
    }

    public struct ObstacleSquare
    {
        public VectorExtensions.Square Square { get; set; }

        public ObstacleSquare(Vector3 position, Quaternion rotation, Vector3 extents)
        {
            ObstacleLayout layout = new ObstacleLayout(position, rotation, extents);

            VectorExtensions.Line line1 = new VectorExtensions.Line(layout.LeftBottomPoint, layout.LeftTopPoint);
            VectorExtensions.Line line2 = new VectorExtensions.Line(layout.RightBottomPoint, layout.RightTopPoint);

            Square = new VectorExtensions.Square(line1, line2);
        }

        public ObstacleSquare(ObstacleLayout layout)
        {
            VectorExtensions.Line line1 = new VectorExtensions.Line(layout.LeftBottomPoint, layout.LeftTopPoint);
            VectorExtensions.Line line2 = new VectorExtensions.Line(layout.RightBottomPoint, layout.RightTopPoint);

            Square = new VectorExtensions.Square(line1, line2);
        }
    }
}