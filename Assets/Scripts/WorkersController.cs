using BeziWebSocketSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorkersController : MonoBehaviour
{
    [Header("Worker spawning")]
    [SerializeField] GameObject workerPrefab;
    [SerializeField] Transform homeBase;
    [SerializeField] Transform homeEntrance;
    [SerializeField] Transform pedestrianHomeEntrance;
    [SerializeField] Transform backEntrance;
    [SerializeField] Transform hireBase;


    [Header("Job Market Rotation")]
    [SerializeField] int maxJobMarketWorkers = 10;
    [SerializeField] int monthlyRotationCount = 5;

    private List<Worker> hiredWorkers = new List<Worker>();
    private List<Worker> jobMarketWorker = new List<Worker>();

    private Worker selectedWorker;

    //Singleton 
    private static WorkersController _instance;

    public static WorkersController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<WorkersController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<WorkersController>().Length > 1)
                {
                    Debug.LogWarning("More than one WorkersController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("WorkersController");
                    _instance = singletonObject.AddComponent<WorkersController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of WorkersController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // If an instance already exists, destroy this GameObject
            Destroy(gameObject);
        }

        TimeController.Instance.OnMonthPassed += HandleMonthPassed;
    }

    private void OnDestroy()
    {
        TimeController.Instance.OnMonthPassed -= HandleMonthPassed;
    }

    private void Start()
    {
        //Debug: hire workers at the beginning
        //for (int i = 0; i < 10; i++)
        //{
        //    Worker worker = AddWorkerForHire();
        //    HireWorker(worker);
        //}

        //Add workers for hire too 
        for (int i = 0; i < 10; i++)
        {
            AddWorkerForHire();
        }
    }

    public void HandleLoad()
    {
        List<WorkerData> workers = SaveController.Instance.GetData().Workers;

        foreach (WorkerData workerData in workers)
        {
            Worker worker = LoadWorker(workerData);
            HireWorker(worker);
            worker.ResumeWorkerTask();

            worker.OnStatusChange?.Invoke();
        }
    }

    private void RotateJobMarketWorkers()
    {
        // Remove oldest N workers from job market
        for (int i = 0; i < monthlyRotationCount && jobMarketWorker.Count > 0; i++)
        {
            Worker toRemove = jobMarketWorker[0];
            UIManager.Instance.RemoveWorkerFromJobMarketList(toRemove); // You should implement this if not already
            Destroy(toRemove.gameObject);
            jobMarketWorker.RemoveAt(0);
        }

        // Add new workers to maintain the cap
        while (jobMarketWorker.Count < maxJobMarketWorkers)
        {
            AddWorkerForHire();
        }

        Debug.Log($"[WorkersController] Rotated job market workers. New count: {jobMarketWorker.Count}");
    }

    public Worker AddWorkerForHire()
    {
        //Create worker
        GameObject workerObj = Instantiate(workerPrefab, hireBase);
        Worker worker = workerObj.GetComponent<Worker>();

        //Randomize it's stats 
        RandomizeWorker(worker);

        UIManager.Instance.AddWorkerToJobMarketList(worker);
        jobMarketWorker.Add(worker);

        return worker;
    }

    public Worker LoadWorker(WorkerData workerData)
    {
        //Create worker
        GameObject workerObj = Instantiate(workerPrefab, homeBase);
        Worker worker = workerObj.GetComponent<Worker>();
        worker.LoadFromData(workerData);

        //worker.OnStatusChange?.Invoke();

        return worker;
    }

    

    public void HireWorker(Worker worker)
    {
        //Set position
        worker.transform.SetParent(homeBase);
        worker.transform.localPosition = Vector3.zero;
        worker.homeBase = homeBase;
        worker.homeEntrance = pedestrianHomeEntrance;
        worker.backEntrance = backEntrance;

        //Set its UI
        UIManager.Instance.AddWorkerToHiredLists(worker);
        UIManager.Instance.RemoveWorkerFromJobMarketList(worker);

        //Add to the list
        hiredWorkers.Add(worker);
        if (jobMarketWorker.Contains(worker))
            jobMarketWorker.Remove(worker);
    }

    private void RandomizeWorker(Worker pWorker)
    {
        //3 random names
        int rngName = UnityEngine.Random.Range(0, 3);
        switch (rngName)
        {
            case 0:
                pWorker.workerName = "Charlie";
                break;
            case 1:
                pWorker.workerName = "Max";
                break;
            case 2:
                pWorker.workerName = "Alex";
                break;
            default:
                pWorker.workerName = "John";
                break;
        }

        pWorker.name = pWorker.workerName;

        //TODO: Random icons and models

        //Random stats
        int rngStat1 = UnityEngine.Random.Range(3, 11);
        int rngStat2 = UnityEngine.Random.Range(3, 11);
        pWorker.skills.Add("Skill1", rngStat1);
        pWorker.skills.Add("Skill2", rngStat2);
        pWorker.salary = UnityEngine.Random.Range(3000, 4001);
    }

    public void SelectWorker(Worker pWorker)
    {
        selectedWorker = pWorker;
    }

    public void AssignWorker(Worker pWorker, Contract pContract, Transport transport = null)
    {
        print($"Worker was assigned: {pWorker} to {pContract}");

        // Try get transport
        pWorker.AssignContract(pContract, transport);
    }

    public void AssignWorkers(List<Worker> workers, Contract pContract)
    {
        if (workers.Count == 0)
        {
            UnityEngine.Debug.LogError("Workers count is 0");
            return;
        }
        print("Assigning workers");

        //// First assign the leader for everyone
        //workers[0].isLeader = true;

        //for (int i = 1; i < workers.Count; i++)
        //{
        //    workers[i].SetLeader(workers[0]);
        //}

        // Get the neccessary equipment
        Queue<Transport> neededEquipment = new Queue<Transport>();
        foreach (ItemInstance item in pContract.roadDamage.assignedEquipment.Instances)
        {
            EquipmentInstance equipment = item as EquipmentInstance;
            if (equipment != null)
            {
                Transport transport = equipment.Transport;
                if (transport != null)
                    neededEquipment.Enqueue(equipment.Transport);
                else
                    Debug.LogError("Transport is null");
            }
            else
                Debug.LogError("Cast failed");
        }

        int workersToTrucks = workers.Count - neededEquipment.Count; // Workers not using equipment
        List<Transport> neededTrucks = InventoryController.Instance.GetNeededTrucks(workersToTrucks);

        // Track how many workers are assigned to each truck
        Dictionary<Transport, int> truckAssignments = new Dictionary<Transport, int>();

        Transport currentTruck = neededTrucks.Count > 0 ? neededTrucks[0] : null;

        foreach (Worker worker in workers)
        {
            Transport transportForWorker = null;

            // Assign equipment first
            if (neededEquipment.Count > 0)
            {
                transportForWorker = neededEquipment.Dequeue();
            }
            // Assign to truck
            else if (neededTrucks.Count > 0)
            {
                transportForWorker = currentTruck;

                if (!truckAssignments.ContainsKey(currentTruck))
                    truckAssignments[currentTruck] = 0;

                truckAssignments[currentTruck]++;

                // If truck is full, move to the next one
                if (truckAssignments[currentTruck] >= 4)
                {
                    neededTrucks.RemoveAt(0);
                    currentTruck = neededTrucks.Count > 0 ? neededTrucks[0] : null;
                }
            }

            AssignWorker(worker, pContract, transportForWorker);
        }
    }

    public List<Worker> GetHiredWorkers()
    {
        return hiredWorkers;
    }

    public string GenerateWorkerID(Worker worker)
    {
        return $"worker_{worker.workerName}_{hiredWorkers.Count.ToString()}";
    }

    private void HandleMonthPassed(DateTime time)
    {
        RotateJobMarketWorkers();
    }

    public Worker GetWorkerByID(string ID)
    {
        return hiredWorkers.Find(x => x.workerID == ID);
    }
}
