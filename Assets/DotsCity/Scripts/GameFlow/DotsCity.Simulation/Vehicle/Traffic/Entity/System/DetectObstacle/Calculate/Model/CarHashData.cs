using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public struct CarHashData
    {
        /// <summary> Unity.Entities.Entity of the vehicle. </summary>
        public Entity Entity;

        /// <summary> Position of the vehicle. </summary>
        public float3 Position;

        /// <summary> Rotation of the vehicle. </summary>
        public quaternion Rotation;

        /// <summary> Forwarding of the vehicle. </summary>
        public float3 Forward;

        /// <summary> Bounds of the vehicle. </summary>
        public BoundsComponent Bounds;

        /// <summary> Current destination. </summary>
        public float3 Destination;

        /// <summary> Current node entity index to the vehicle (node that is too close to the vehicle). If -1 index then no current node. </summary>
        public int CurrentNodeIndex;

        /// <summary> Destination node entity index. </summary>
        public int DestinationNodeIndex;

        /// <summary> Current global path index. </summary>
        public int PathIndex;

        /// <summary> Local node index of the current path. </summary>
        public int LocalPathNodeIndex;

        /// <summary> Current priority. </summary>
        public int Priority;

        /// <summary> Current speed m/s. </summary>
        public float Speed;

        /// <summary> Distance to end of path. </summary>
        public float DistanceToEnd;

        /// <summary> Distance to waypoint. </summary>
        public float DistanceToWaypoint;

        /// <summary> Current calculated obstacle entity. </summary>
        public Entity ObstacleEntity;

        /// <summary> Current raycasted obstacle entity. </summary>
        public Entity RayObstacleEntity;

        public State States;

        public Entity CurrentObstacleEntity => ObstacleEntity != Entity.Null ? ObstacleEntity : RayObstacleEntity;

        public float3 ForwardPoint => Position + Forward * Bounds.Extents.z;

        public float3 BackwardPoint => Position - Forward * Bounds.Extents.z;
    }
}