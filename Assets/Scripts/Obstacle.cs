using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float obstacleRadius = 2f; // Radius around the obstacle to mark nodes as unwalkable

    void OnEnable()
    {
        GraphNode node = AstarPath.active.GetNearest(transform.position).node;
        node.Walkable = false; // Mark as unwalkable

        // Optionally, disable nearby nodes
        foreach (GraphNode nearbyNode in GetNearbyNodes(transform.position, obstacleRadius))
        {
            nearbyNode.Walkable = false;
        }
    }

    void OnDisable()
    {
        GraphNode node = AstarPath.active.GetNearest(transform.position).node;
        node.Walkable = false; // Mark as unwalkable

        // Optionally, disable nearby nodes
        foreach (GraphNode nearbyNode in GetNearbyNodes(transform.position, obstacleRadius))
        {
            nearbyNode.Walkable = true;
        }
    }

    private IEnumerable<GraphNode> GetNearbyNodes(Vector3 position, float radius)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        NNConstraint constraint = NNConstraint.Default;
#pragma warning restore CS0618 // Type or member is obsolete
        List<GraphNode> nearbyNodes = new List<GraphNode>();
        AstarPath.active.data.pointGraph.GetNodes(node =>
        {
            if (Vector3.Distance((Vector3)node.position, position) < radius)
                nearbyNodes.Add(node);
        });
        return nearbyNodes;
    }
}