using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Panel_JobMarket : Panel
{
    [SerializeField] Transform hireContentparent;
    [SerializeField] GameObject hireListingPrefab;
    private Dictionary<Worker, WorkerListing_Hire> displayedWorkers = new Dictionary<Worker, WorkerListing_Hire>();

    public void AddWorkerHireToList(Worker worker)
    {
        GameObject listingObj = Instantiate(hireListingPrefab, hireContentparent);
        WorkerListing_Hire workerListing = listingObj.GetComponent<WorkerListing_Hire>();
        workerListing.SetWorker(this, worker);
        workerListing.SetSalary(worker.salary);
        displayedWorkers.Add(worker, workerListing);
    }

    public void RemoveWorkerHireFromList(Worker worker)
    {
        WorkerListing_Hire listing = null;
        bool hasWorker = displayedWorkers.TryGetValue(worker, out listing);

        if (hasWorker)
        {
            displayedWorkers.Remove(worker);
            Destroy(listing.gameObject);
        }
    }
}
