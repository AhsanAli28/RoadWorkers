using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

[System.Serializable]
public class WorkerEntityData
{
    public Entity workerEntity;
    public Worker workerGameObject;
    public Entity currentTargetNode;
    public bool isActive;
}

public class WorkerEntityTracker : MonoBehaviour
{
    [SerializeField] private List<WorkerEntityData> activeWorkers = new List<WorkerEntityData>();
    [SerializeField] private WorkerEntitySpawner entitySpawner;

    private void Start()
    {
        if (entitySpawner == null)
            entitySpawner = FindObjectOfType<WorkerEntitySpawner>();
    }

    public Entity SpawnWorkerEntity(Worker workerGameObject, Vector3 spawnPosition, Vector3 targetPosition)
    {
        // Find closest nodes to spawn and target positions
        Entity spawnNode = entitySpawner.FindClosestPedestrianNode(spawnPosition);
        Entity targetNode = entitySpawner.FindClosestPedestrianNode(targetPosition);

        if (spawnNode == Entity.Null || targetNode == Entity.Null)
        {
            Debug.LogError("Could not find pedestrian nodes near the specified positions!");
            return Entity.Null;
        }

        // Spawn the entity
        Entity workerEntity = entitySpawner.SpawnWorkerAtNode(spawnNode, targetNode);

        // Track the worker
        var workerData = new WorkerEntityData
        {
            workerEntity = workerEntity,
            workerGameObject = workerGameObject,
            currentTargetNode = targetNode,
            isActive = true
        };

        activeWorkers.Add(workerData);

        // Hide the GameObject version while entity is active
        //workerGameObject.gameObject.SetActive(false);

        return workerEntity;
    }

    public bool HasWorkerReachedDestination(Worker workerGameObject)
    {
        var workerData = activeWorkers.Find(w => w.workerGameObject == workerGameObject);

        if (workerData != null && workerData.isActive)
        {
            return entitySpawner.HasWorkerReachedDestination(workerData.workerEntity);
        }

        return false;
    }

    public void CompleteWorkerTask(Worker workerGameObject, Vector3 returnPosition)
    {
        var workerData = activeWorkers.Find(w => w.workerGameObject == workerGameObject);

        if (workerData != null && workerData.isActive)
        {
            // Move GameObject to final position
            workerGameObject.transform.position = returnPosition;
            //workerGameObject.gameObject.SetActive(true);

            // Mark as inactive
            workerData.isActive = false;

            Debug.Log($"Worker {workerGameObject.name} task completed!");
        }
    }
}
