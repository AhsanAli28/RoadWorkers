using System;
using UnityEngine;
using Pathfinding;
using System.Collections.Generic;
using BeziWebSocketSharp;

public enum WorkerStatus
{
    Available,
    Moving,
    Working,
    Returning,
    Resting,
    Delivering
}

public class Worker : MonoBehaviour
{
    // Events
    public Action OnStatusChange;

    // Stats
    public string workerID;
    public string workerName;
    public int energy = 100;
    public int salary = 3000;
    public float workModifier = 1f;
    public Dictionary<string, int> skills = new Dictionary<string, int>();
    public Transform homeEntrance;
    public Transform backEntrance;
    public Transform homeBase;

    // State
    public WorkerStatus status = WorkerStatus.Available;
    private Contract assignedContract;
    [SerializeField] private RoadDamage assignedRoadDamage;
    private WorkerData workerData;

    [Header("Flags")]
    //public bool isLeader = false;
    //private Worker leader;
    public bool arrivedToWorkFlag = false;
    private bool arrivedToPickup = false;

    // Pathfinding/UI
    [Header("References")]
    [SerializeField] AIPath aiNav;
    [SerializeField] Seeker seeker;
    [SerializeField] AIDestinationSetter destinationSetter;
    [SerializeField] GameObject humanModel;
    [SerializeField] GameObject carModel;
    [SerializeField] Animator animator;

    // Transport
    [SerializeField] private Transport assignedTransport;
    private bool isInTransport = false;
    public Transport transportToPickUp;
    public EquipmentInstance transportToPickUpInstance;

    private void Update()
    {
        switch (status)
        {
            case WorkerStatus.Available: break;
            case WorkerStatus.Moving: HandleMoving(); break;
            case WorkerStatus.Working: HandleWorking(); break;
            case WorkerStatus.Returning: HandleReturning(); break;
            case WorkerStatus.Resting: HandleResting(); break;
            case WorkerStatus.Delivering: HandleDelivering(); break;
        }
    }

    private void SetStatus(WorkerStatus newStatus)
    {
        status = newStatus;
        OnStatusChange?.Invoke();
        SaveData();
    }

    // FSM Handlers

    // Moving to assigned contract specifically
    private void HandleMoving()
    {
        if (assignedContract == null || assignedRoadDamage == null)
        {
            BeginReturnTrip();
            return;
        }

        // Move to transport
        if (assignedTransport != null && !isInTransport && !arrivedToWorkFlag)
        {
            if (Vector3.Distance(transform.position, assignedTransport.EntryPoint.position) <= 4f)
            {
                EnterTransport();
                SetTransportWorkDestination();
            }
            return;
        }

        // Sit and wait in transport
        if (isInTransport)
        {
            FollowTransport();
            return;
        }

        // No transport, walking to work behaviour
        if (assignedTransport == null && assignedContract != null)
        {
            if (aiNav.reachedDestination)
            {
                OnArrivedToWork();
            }
        }
    }

    private void HandleWorking()
    {
        if (assignedContract == null || assignedRoadDamage == null)
        {
            BeginReturnTrip();
            return;
        }

        assignedRoadDamage.ProgressWork(Time.deltaTime * workModifier);

        if (assignedRoadDamage.IsCompleted())
        {
            assignedContract.CompleteContract();
            assignedContract.OnContractStateChange -= HandleContractChange;
            assignedContract = null;
            assignedRoadDamage = null;

            BeginReturnTrip();
        }
    }

    private void HandleReturning()
    {
        if (assignedTransport != null && isInTransport)
        {
            FollowTransport();
        }
        else
        {
            // Walking home
            if (aiNav.reachedDestination)
            {
                OnArrivedHome();
            }
        }
    }

    private void HandleDelivering()
    {
        // Walking to market
        if (!arrivedToPickup && aiNav.reachedDestination)
        {
            OnArrivedToPickup();
            return;
        }

        if (arrivedToPickup && !aiNav.reachedDestination && assignedTransport != null)
        {
            FollowTransport();
            return;
        }
    }

    private void HandleResting()
    {
        SetStatus(WorkerStatus.Available);
    }

    // PUBLIC API

    public void SetContract(Contract contract)
    {
        CancelContract();

        assignedContract = contract;
        contract.OnContractStateChange += HandleContractChange;

        if (contract.roadDamage != null && contract.roadDamage != null)
        {
            assignedRoadDamage = contract.roadDamage;
            assignedRoadDamage.AssignWorker(this);
        }
        else
            throw new Exception("No roadDamages on contract");
    }

    public void CancelContract()
    {
        if (assignedContract != null)
            assignedContract.OnContractStateChange -= HandleContractChange;

        assignedContract = null;
        assignedRoadDamage = null;
    }

    public void AssignContract(Contract contract, Transport pAssignedTransport = null)
    {
        if (contract == null)
        {
            Debug.LogError("Contract was null");
            return;
        }

        SetContract(contract);

        if (pAssignedTransport != null)
        {
            AssignTransport(pAssignedTransport);
            transform.position = backEntrance.position;
            transform.rotation = backEntrance.rotation;
        }
        else
        {
            // No transport? Walk directly to site
            BeginWalkToWork();
        }

        arrivedToWorkFlag = false;

        SetStatus(WorkerStatus.Moving);
    }



    public void AssignTransport(Transport pAssignedTransport)
    {
        if (pAssignedTransport == null)
            return;

        assignedTransport = pAssignedTransport;
        assignedTransport.expectingWorkersCount++;

        // Update its availability
        InventoryController.Instance.BorrowTransport(pAssignedTransport);

        MoveToTransport();
    }

    public void AssignDelivery(Transport pTransportToPickup, EquipmentInstance equipment)
    {
        SetStatus(WorkerStatus.Delivering);

        transportToPickUp = pTransportToPickup;
        transportToPickUpInstance = equipment;
        arrivedToPickup = false;
        BeginWalkToPickup();
    }

    private void BeginWalkToPickup()
    {
        transform.position = homeEntrance.position;
        transform.rotation = homeEntrance.rotation;

        // Animation
        humanModel.SetActive(true);
        PlayWalkingAnimation();

        destinationSetter.target = MarketController.Instance.equipmentMarketPedestrianEntry;
        aiNav.canMove = true;
    }

    // HELPERS

    private void MoveToTransport()
    {
        aiNav.canMove = true;
        destinationSetter.target = assignedTransport.EntryPoint;
        humanModel.SetActive(true);
        PlayWalkingAnimation();
    }

    private void SetTransportWorkDestination()
    {
        assignedTransport.OnArrived += OnArrivedToWork;

        if (assignedTransport.expectingWorkersCount <= 0) // When all the workers entered the transport, send it off 
        {
            assignedTransport.StartNavigation(assignedRoadDamage.destinationPoint.position, null);
        }
    }

    private void SetTransportHomeDestination()
    {
        assignedTransport.OnArrived += OnArrivedToFleet;

        if (assignedTransport.expectingWorkersCount <= 0) // When all the workers entered the transport, send it off 
        {
            Vector3 parkingSpotPos = CompanyController.Instance.GetParkingByID(assignedTransport.GetAssignedParkingSpotID()).transform.position;
            assignedTransport.StartNavigation(parkingSpotPos, null);
            print($"Set destination to home: {parkingSpotPos}");
        }
    }

    private void EnterTransport()
    {
        isInTransport = true;
        aiNav.canMove = false;
        destinationSetter.target = null;
        humanModel.SetActive(false);
        transform.position = assignedTransport.transform.position;
        assignedTransport.AddPassanger(this);
    }

    private void ExitTransport()
    {
        if (assignedTransport == null)
            return;

        isInTransport = false;
        humanModel.SetActive(true);
        PlayIdleAnimation();
        transform.position = assignedTransport.transform.position;
        assignedTransport.RemovePassanger(this);
    }

    private void OnArrivedToWork()
    {
        // Wrong call and work was already completed
        if (assignedRoadDamage == null)
            return;

        if (assignedTransport != null)
        {
            //assignedTransport.ClearOnArrivedListeners();
            //assignedTransport.ClearOnArrivedListeners();
            // Disable arrived transport on arrival for simplicity
            assignedTransport.ToggleTrnasport(false);
            assignedTransport.transform.position = assignedRoadDamage.transform.position;
        }

        isInTransport = false;
        aiNav.canMove = false;
        destinationSetter.target = null;
        arrivedToWorkFlag = true;
        assignedRoadDamage.ToggleConstructionSite(true);

        // Setup on the position
        if (assignedTransport != null && assignedRoadDamage.IsEquipmentPartOfConstruction(assignedTransport.ItemID))
        {
            // As if we are inside the operating equipment
            humanModel.SetActive(false);
            transform.position = assignedTransport.transform.position;

            // Setup equipment on road damage
            print("Trying to enable equipment");
            assignedRoadDamage.EnableEquipemnt(assignedTransport.ItemID);
        }
        else
        {
            // Place a pedestrian worker
            WorkingPointAnim workPoint = assignedRoadDamage.AssignWorkPoint();
            if (workPoint != null)
            {
                humanModel.SetActive(true);
                transform.position = workPoint.transform.position;
                transform.rotation = workPoint.transform.rotation;
                PlayWorkingAnimation(workPoint.AnimationID);
            }
            else // When ran out of positions
            {
                humanModel.SetActive(false);
                transform.position = assignedRoadDamage.transform.position;
            }
        }

        // When all workers arrived to work
        if (assignedRoadDamage.isReadyToStart)
        {
            assignedRoadDamage.BeginConstruction();
            assignedContract.StartContract();
            assignedRoadDamage.ToggleProgressBar(true);

            foreach (Worker worker in assignedRoadDamage.assignedWorkers)
            {
                worker.StartWork();
            }
        }
    }

    private void OnArrivedToFleet()
    {
        if (assignedTransport)
            assignedTransport.Park();
        ExitTransport();
        aiNav.canMove = true;
        destinationSetter.target = backEntrance;

        // Animation
        humanModel.SetActive(true);
        PlayWalkingAnimation();

        if (assignedTransport)
        {
            //assignedTransport.ClearOnArrivedListeners();
            //assignedTransport.ClearOnArrivedListeners();
        }
        assignedTransport = null;
        SaveData();
    }

    private void OnDeliveredEquipment()
    {
        ExitTransport();
        //assignedTransport.ClearOnArrivedListeners();
        //assignedTransport.ClearOnArrivedListeners();
        MarketController.Instance.DeliverEquipment(transportToPickUpInstance);
        transportToPickUp = null;
        transportToPickUpInstance = null;
        arrivedToPickup = false;
        SetStatus(WorkerStatus.Returning);
    }

    private void OnArrivedHome()
    {
        aiNav.canMove = false;
        destinationSetter.target = null;

        transform.position = homeBase.position;

        SetStatus(WorkerStatus.Resting);
    }

    private void OnArrivedToPickup()
    {
        arrivedToPickup = true;
        assignedTransport = transportToPickUp;
        assignedTransport.ToggleTrnasport(true); // It was hidden by the market for simplicity
        EnterTransport();
        assignedTransport.OnArrived += OnDeliveredEquipment;
        SetTransportHomeDestination();
    }

    private void StartWork()
    {
        SetStatus(WorkerStatus.Working);
    }

    private void BeginReturnTrip()
    {
        SetStatus(WorkerStatus.Returning);
        arrivedToWorkFlag = false;

        if (assignedTransport != null)
        {
            assignedTransport.ToggleTrnasport(true);
            EnterTransport();
            SetTransportHomeDestination();
        }
        else
        {
            BeginWalkToHome();
        }
    }

    private void BeginWalkToWork()
    {
        transform.position = homeEntrance.position;
        transform.rotation = homeEntrance.rotation;

        // Animation
        humanModel.SetActive(true);
        PlayWalkingAnimation();

        destinationSetter.target = assignedRoadDamage.pedestrianDestinationPoint;
        aiNav.canMove = true;
    }

    private void BeginWalkToHome()
    {
        // Animation
        humanModel.SetActive(true);
        PlayWalkingAnimation();

        destinationSetter.target = homeEntrance.transform;
        aiNav.canMove = true;
    }

    private void FollowTransport()
    {
        if (assignedTransport != null)
        {
            humanModel.SetActive(false);
            transform.position = assignedTransport.transform.position;
        }
    }

    private void HandleContractChange(Contract contract)
    {
        if (assignedContract == contract && (contract.state == ContractState.Canceled))
        {
            CancelContract();
            BeginReturnTrip();
        }
    }

    #region Animation

    public void PlayWalkingAnimation()
    {
        if (animator == null)
            return;

        animator.Play("Walking");
    }

    public void PlayWorkingAnimation(int animationID)
    {
        if (animator == null)
            return;

        switch (animationID)
        {
            case 0:
                animator.Play("Idle");
                break;
            case 1:
                animator.Play("Tablet");
                break;
            case 2:
                animator.Play("Working");
                break;
            case 3:
                animator.Play("Kneeling");
                break;
            default:
                Debug.LogWarning("Animation ID is out of range");
                animator.Play("Idle");
                break;
        }

    }

    public void PlayIdleAnimation()
    {
        if (animator == null)
            return;

        animator.Play("Idle");
    }

    #endregion

    #region Saving 

    public WorkerData CreateWorkerData()
    {
        WorkerData data = new WorkerData();
        data.WorkerID = WorkersController.Instance.GenerateWorkerID(this);
        data.Name = workerName;
        data.Energy = energy;
        data.Salary = salary;
        data.Status = status;
        data.Skills = new SerializableDictionary<string, int>(skills);

        if (assignedRoadDamage != null)
            data.AssignedRoadDamageID = assignedRoadDamage.RoadDamageID;
        if (assignedTransport != null)
            data.AssignedTransportID = InventoryController.Instance.GetTransportInstanceID(assignedTransport);

        return data;
    }

    public void SaveData()
    {
        if (workerData == null)
        {
            workerData = CreateWorkerData();
            workerID = workerData.WorkerID;

            SaveController.Instance.UpdateData(gameData =>
            {
                gameData.Workers.Add(workerData);
            });
        }
        else
        {
            // Just update the fields            
            workerData.Name = workerName;
            workerData.Energy = energy;
            workerData.Salary = salary;
            workerData.Status = status;
            workerData.Skills = new SerializableDictionary<string, int>(skills);
            workerData.AssignedRoadDamageID = assignedRoadDamage != null ? assignedRoadDamage.RoadDamageID : null;
            workerData.AssignedTransportID = assignedTransport != null ? InventoryController.Instance.GetTransportInstanceID(assignedTransport) : null;
        }
    }


    public void LoadFromData(WorkerData data)
    {
        workerData = data;

        workerID = data.WorkerID;
        workerName = data.Name;
        energy = data.Energy;
        salary = data.Salary;
        status = data.Status;
        skills = new Dictionary<string, int>(data.Skills.ToDictionary());

        // Assigned contract/damage
        if (!data.AssignedRoadDamageID.IsNullOrEmpty())
        {
            assignedRoadDamage = RoadsController.Instance.GetRoadDamage(data.AssignedRoadDamageID);
            if (assignedRoadDamage != null)
                assignedContract = assignedRoadDamage.GetAssignedContract();
            else
                Debug.LogError("Assigned RoadDamage was null");
        }

        // Assigned transport
        if (!data.AssignedTransportID.IsNullOrEmpty())
        {
            EquipmentInstance equipmentInstance = InventoryController.Instance.GetEquipmentByID(data.AssignedTransportID);
            if (equipmentInstance != null)
                assignedTransport = equipmentInstance.Transport;
            else
                Debug.LogError("Equipment Instance was null");

        }
    }

    // Used for loading
    public void ResumeWorkerTask()
    {
        switch (status)
        {
            case WorkerStatus.Available:
                // Do nothing
                break;
            case WorkerStatus.Moving:
                // Re-assign the contract and move from start for simplicity
                ReassignContract();
                break;
            case WorkerStatus.Working:
                // Move to the assigned RoadDamage and continue work
                ResumeRoadDamage();
                break;
            case WorkerStatus.Returning:
                // Insta return for simplicity
                if (assignedTransport != null)
                    OnArrivedToFleet();
                else
                    OnArrivedHome();
                break;
            case WorkerStatus.Resting:
                // Do Nothing
                break;
            case WorkerStatus.Delivering:
                // Handled by Market Controller
                break;
            default:
                break;
        }
    }

    public void ReassignContract()
    {
        if (assignedContract == null)
        {
            Debug.LogError("Assigned Contract was null");
            return;
        }

        AssignContract(assignedContract, assignedTransport);
    }

    public void ResumeRoadDamage()
    {
        if (assignedRoadDamage == null)
        {
            Debug.LogError("Road Damage was null");
            return;
        }

        assignedRoadDamage.AssignWorker(this);
        //assignedRoadDamage.EnableAllEquipment();
        OnArrivedToWork();
    }

    #endregion
}

[Serializable]
public class WorkerData
{
    public string WorkerID;
    public string Name;
    public int Energy;
    public int Salary;
    public SerializableDictionary<string, int> Skills = new SerializableDictionary<string, int>();
    public WorkerStatus Status;
    public string AssignedRoadDamageID = null;
    public string AssignedTransportID = null;
}