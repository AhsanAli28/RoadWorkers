using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static Spirit604.DotsCity.Simulation.Common.EntitySelectionService;

public class TransportController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform transportParent;

    [Header("Transports")]
    [SerializeField] GameObject _transport_truck;
    [SerializeField] GameObject _transport_van;
    [SerializeField] GameObject _transport_dumpTruck;
    [SerializeField] GameObject _transport_miller;
    [SerializeField] GameObject _transport_excavator;
    [SerializeField] GameObject _transport_paver;

    [Header("Pedestrians")]
    [SerializeField] EntityCustomPathServiceBase pedestrianPathService;

    //Singleton 
    private static TransportController _instance;

    public static TransportController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<TransportController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<TransportController>().Length > 1)
                {
                    Debug.LogWarning("More than one TransportController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("TransportController");
                    _instance = singletonObject.AddComponent<TransportController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of TransportController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void SendTransport(
        Vector3 startPosition,
        Vector3 endPosition,
        Action onDestinationReached,
        Transport transport = null,
        TransportType type = TransportType.Truck,
        Worker drivingWorker = null,
        bool returnAfterArrival = false,
        bool destroyAfterReturn = false,
        Action onReturnReached = null)
    {

        if (transport == null)
        {
            GameObject transportObj = SpawnTransport(type, startPosition);
            transport = transportObj.GetComponent<Transport>();
        }

        transport.SetOnReturnArrival(onReturnReached);
        transport.StartNavigation(endPosition, onDestinationReached, returnAfterArrival, destroyAfterReturn);
    }

    public GameObject SpawnTransport(TransportType type, Vector3 position)
    {
        GameObject transport = null;
        switch (type)
        {
            case TransportType.Truck:
                transport = Instantiate(_transport_truck, transportParent);
                break;
            case TransportType.Van:
                transport = Instantiate(_transport_van, transportParent);
                break;
            case TransportType.DumpTruck:
                transport = Instantiate(_transport_dumpTruck, transportParent);
                break;
            case TransportType.Miller:
                transport = Instantiate(_transport_miller, transportParent);
                break;
            case TransportType.Excavator:
                transport = Instantiate(_transport_excavator, transportParent);
                break;
            case TransportType.Paver:
                transport = Instantiate(_transport_paver, transportParent);
                break;
            default:
                break;
        }

        if (transport != null)
        {
            transport.transform.position = position;
        }
        else
        {
            Debug.LogWarning("Spawned transport is null");
        }

        return transport;
    }

    ///// <summary>
    ///// Automatically retrieves the entities based on their positions 
    ///// </summary>
    ///// <param name="entity"></param>
    ///// <param name="endNode"></param>
    //public void SendPedestrian(Vector3 pedestrianPosition, Vector3 destinationPosition)
    //{
    //    Entity pedestrianEntity = RetrievePedestrianEntity(pedestrianPosition);
    //    Entity destinationEntity = RetriveDestinationEntity(destinationPosition);
    //    print($"Pedestrian entity: {pedestrianEntity}");
    //    print($"Destination entity: {destinationEntity}");
    //    pedestrianPathService.SetFollowPath(pedestrianEntity, destinationEntity);
    //}

    //private Entity RetrievePedestrianEntity(Vector3 position)
    //{
    //    return EntitySelectionService.Instance.SelectEntity(position, EntityType.Any, 0.5f);
    //}

    //private Entity RetriveDestinationEntity(Vector3 position)
    //{
    //    return EntitySelectionService.Instance.SelectEntity(position, EntityType.Any, 0.1f);
    //}

    /// <summary>
    /// Uses Entity.
    /// </summary>
    /// <param name="worker"></param>
    /// <param name="destination"></param>
    public void SendWorker(Worker worker, Vector3 destination)
    {
        var workerTracker = FindObjectOfType<WorkerEntityTracker>();

        if (workerTracker != null)
        {
            // Spawn entity at worker's current position, send to destination
            Entity workerEntity = workerTracker.SpawnWorkerEntity(
                worker,
                worker.transform.position,
                destination
            );

            if (workerEntity != Entity.Null)
            {
                Debug.Log($"Spawned worker entity for {worker.name} heading to {destination}");
            }
        }
    }

    public bool HasWorkerReachedDestination(Worker worker)
    {
        var workerTracker = FindObjectOfType<WorkerEntityTracker>();
        return workerTracker != null && workerTracker.HasWorkerReachedDestination(worker);
    }


    /// <summary>
    /// False = small, True = large.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool GetTransportSize(TransportType type)
    {
        switch (type)
        {
            case TransportType.None:
                return false;
            case TransportType.Truck:
                return false;
            case TransportType.Van:
                return false;
            case TransportType.DumpTruck:
                return true;
            case TransportType.Miller:
                return true;
            case TransportType.Excavator:
                return true;
            case TransportType.Paver:
                return true;
            default:
                return false;
        }
    }
}


public enum TransportType
{
    None,
    Truck,
    Van,
    DumpTruck,
    Miller,
    Excavator,
    Paver
}
