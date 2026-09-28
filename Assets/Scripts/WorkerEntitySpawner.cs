using UnityEngine;
using Unity.Entities;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Common;

public class WorkerEntitySpawner : MonoBehaviour
{
    [Header("Services")]
    //[SerializeField] private PedestrianSpawnService pedestrianSpawnService;
    //[SerializeField] private PedestrianCustomPathService pedestrianPathService;

    private EntityManager entityManager;

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Find services if not assigned
        //if (pedestrianSpawnService == null)
        //    pedestrianSpawnService = PedestrianSpawnService.Instance;

        //if (pedestrianPathService == null)
        //    pedestrianPathService = PedestrianCustomPathService.Instance;


        if (PedestrianSpawnService.Instance == null)
        {
            Debug.LogError("Custom Spawn Service is null");
        }

        if (PedestrianCustomPathService.Instance == null)
        {
            Debug.LogError("Custom Path Service is null");
        }
    }

    /// <summary>
    /// Spawn a worker entity at a specific pedestrian node
    /// </summary>
    /// <param name="spawnNodeEntity">The node entity where to spawn the worker</param>
    /// <param name="targetNodeEntity">The target node entity where the worker should go</param>
    /// <param name="skinIndex">Optional skin variation (-1 for random)</param>
    /// <returns>The spawned worker entity</returns>
    public Entity SpawnWorkerAtNode(Entity spawnNodeEntity, Entity targetNodeEntity, int skinIndex = -1)
    {
        if (spawnNodeEntity == Entity.Null || targetNodeEntity == Entity.Null)
        {
            Debug.LogError("Invalid node entities provided!");
            return Entity.Null;
        }

        // Store the target for later use
        Entity workerEntity = Entity.Null;

        // Subscribe to spawning completion to get the entity reference
        System.Action<Entity> onSpawnComplete = (spawnedEntity) =>
        {
            workerEntity = spawnedEntity;
            StartCoroutine(SetDestinationAfterSpawn(spawnedEntity, targetNodeEntity));
        };

        // Spawn the pedestrian entity at the specified node
        PedestrianSpawnService.Instance.Spawn(spawnNodeEntity, skinIndex);

        // Return entity (note: might be null initially, gets set async)
        return workerEntity;
    }

    /// <summary>
    /// Set destination for an already spawned entity
    /// </summary>
    public void SendWorkerToNode(Entity workerEntity, Entity targetNodeEntity)
    {
        if (workerEntity == Entity.Null || targetNodeEntity == Entity.Null)
        {
            Debug.LogError("Invalid entities provided!");
            return;
        }

        if (PedestrianCustomPathService.Instance != null)
        {
            PedestrianCustomPathService.Instance.SetFollowPath(workerEntity, targetNodeEntity);
            Debug.Log($"Worker entity sent to target node: {targetNodeEntity}");
        }
    }

    /// <summary>
    /// Check if a worker has reached its destination
    /// </summary>
    public bool HasWorkerReachedDestination(Entity workerEntity)
    {
        if (workerEntity == Entity.Null || PedestrianCustomPathService.Instance == null)
            return false;

        return !PedestrianCustomPathService.Instance.IsFollowingPath(workerEntity);
    }

    /// <summary>
    /// Find the closest pedestrian node to a world position
    /// </summary>
    public Entity FindClosestPedestrianNode(Vector3 worldPosition, float maxDistance = 10f)
    {
        // Use EntitySelectionService or implement custom logic
        return EntitySelectionService.Instance?.SelectEntity(worldPosition, EntitySelectionService.EntityType.Any, maxDistance) ?? Entity.Null;
    }

    private System.Collections.IEnumerator SetDestinationAfterSpawn(Entity spawnedEntity, Entity targetNodeEntity)
    {
        // Wait a frame to ensure entity is fully initialized
        yield return null;

        // Set the destination
        SendWorkerToNode(spawnedEntity, targetNodeEntity);
    }
}
