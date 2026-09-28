using JetBrains.Annotations;
using Pathfinding;
using SharpToken;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(AIPath), typeof(AIDestinationSetter))]
public class Transport : MonoBehaviour
{
    [SerializeField] private string itemID;
    public string ItemID => itemID;

    public TransportType transportType;
    public bool isLarge;
    [SerializeField] Transform entryPoint;
    [SerializeField] GameObject model;
    public Transform EntryPoint => entryPoint;

    private AIPath aiPath;
    private AIDestinationSetter destinationSetter;

    public event Action OnArrived;
    private event Action onInitialArrival;
    private event Action onReturnArrival;

    private Vector3 startPosition;
    private bool autoReturn = false;
    private bool isReturning = false;
    private bool destroyOnReturn = false;
    private int assignedParkingSpotId;

    private GameObject tempTarget;

    //Workers
    public List<Worker> passangers = new List<Worker>();
    public int expectingWorkersCount;

    private void Awake()
    {
        aiPath = GetComponent<AIPath>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        isLarge = TransportController.Instance.GetTransportSize(transportType);
    }

    public void StartNavigation(Vector3 destination, Action onReached, bool autoReturn = false, bool destroyAfterReturn = false)
    {
        startPosition = transform.position;
        onInitialArrival = onReached;
        destroyOnReturn = destroyAfterReturn;

        SetDestination(destination);
        aiPath.canMove = true;
        this.autoReturn = autoReturn;
    }

    private void Update()
    {
        if (aiPath.canMove && aiPath.reachedDestination && IsCloseToDestination())
        {
            print($"OnArrived Call: {itemID}, Action count: {OnArrived?.GetInvocationList().Length}");
            OnArrived?.Invoke();
            aiPath.canMove = false;
            destinationSetter.target = null;

            if (tempTarget != null)
                Destroy(tempTarget);


            if (!isReturning)
            {
                onInitialArrival?.Invoke();
                onInitialArrival = null;
            }

            if (!isReturning && autoReturn)
            {
                // First arrival
                onInitialArrival?.Invoke();
                onInitialArrival = null;
                // Begin return trip
                isReturning = true;
                SetDestination(startPosition);
                aiPath.canMove = true;
            }
            else
            {
                // Finished return
                onReturnArrival?.Invoke();
                onReturnArrival = null;
                if (destroyOnReturn)
                {
                    Destroy(gameObject);
                }
            }

            ClearOnArrivedListeners();
        }
    }

    private void SetDestination(Vector3 position)
    {
        tempTarget = new GameObject("TransportTarget");
        tempTarget.transform.position = position;
        destinationSetter.target = tempTarget.transform;
    }

    public void SetOnReturnArrival(Action callback)
    {
        onReturnArrival = callback;
    }

    public bool IsCloseToDestination()
    {
        if (destinationSetter.target == null)
            return false;

        if (Vector3.Distance(destinationSetter.target.position, transform.position) < aiPath.endReachedDistance)
            return true;

        return false;
    }

    public void AddPassanger(Worker worker)
    {
        if (!passangers.Contains(worker))
            passangers.Add(worker);
        expectingWorkersCount--;
        if (expectingWorkersCount < 0)
            expectingWorkersCount = 0;
    }

    public void RemovePassanger(Worker worker)
    {
        if (passangers.Contains(worker))
            passangers.Remove(worker);
    }

    public void SetAssignedParkingSpot(int ID)
    {
        assignedParkingSpotId = ID;
    }

    public void ClearOnArrivedListeners()
    {
        OnArrived = null;
    }

    public void ClearAllListeners()
    {
        OnArrived = null;
        onInitialArrival = null;
        onReturnArrival = null;
    }

    public int GetAssignedParkingSpotID()
    {
        return assignedParkingSpotId;
    }

    public ParkingSpot GetAssignedParkingSpot()
    {
        return CompanyController.Instance.GetParkingByID(assignedParkingSpotId);
    }

    public void Park()
    {
        CompanyController.Instance.ParkTransport(this);
        InventoryController.Instance.ReturnTransport(this);
    }

    public void ToggleTrnasport(bool value)
    {
        model.SetActive(value);
        aiPath.canMove = value;
    }


}
