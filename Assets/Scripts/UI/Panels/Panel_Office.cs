using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class Panel_Office : PanelTabs
{
    [Header("Workers")]
    [SerializeField] WorkersController workersController;
    [SerializeField] GameObject workerListingPrefab;
    [SerializeField] RectTransform workersContent;

    [Header("Contracts")]
    [SerializeField] GameObject contractListingPrefab;
    [SerializeField] RectTransform contractsContent;
    [HideInInspector] public WorkerListing selectedWorkerUI;

    [Header("Upgrades")]
    [SerializeField] List<Popup_UpgradeParking> upgradeParkings;

    private List<ContractListing_Manager> listedContracts = new List<ContractListing_Manager>();


    private void Awake()
    {
        OnPanelClosed += DeselectWorker;
    }

    private void OnDestroy()
    {
        OnPanelClosed -= DeselectWorker;
    }


    public void AddWorkerToList(Worker pWorker)
    {
        GameObject listingObj = Instantiate(workerListingPrefab, workersContent);
        WorkerListing workerListing = listingObj.GetComponent<WorkerListing>();
        workerListing.SetWorker(this, pWorker);
    }

    public void AddContractToList(Contract pContract)
    {
        GameObject listingObj = Instantiate(contractListingPrefab, contractsContent);
        ContractListing_Manager contractUI = listingObj.GetComponent<ContractListing_Manager>();
        contractUI.SetContract(this, pContract);
        listedContracts.Add(contractUI);
    }

    public void RemoveContractFromList(ContractListing pContract)
    {
        ContractListing_Manager contractListing_Manager = pContract as ContractListing_Manager;

        if (contractListing_Manager == null)
            return;

        if (listedContracts.Contains(contractListing_Manager))
            listedContracts.Remove(contractListing_Manager);
    }

    public void SelectWorker(WorkerListing pWorkerUI)
    {
        DeselectWorker();

        selectedWorkerUI = pWorkerUI;
        pWorkerUI.ToggleSelect(true);

        Worker worker = pWorkerUI.GetWorker();
        workersController.SelectWorker(worker);

        //Enable the assign button if the worker is avaiable
        if (worker.status == WorkerStatus.Available)
        {
            foreach (ContractListing_Manager contract in listedContracts)
                contract.ToggleAssignButtonInteract(true);
        }
    }

    public void DeselectWorker()
    {
        if (selectedWorkerUI != null)
            selectedWorkerUI.ToggleSelect(false);

        selectedWorkerUI = null;

        foreach (ContractListing_Manager contract in listedContracts)
            contract.ToggleAssignButtonInteract(false);
    }

    public void AssignWorker(Contract pContract)
    {
        //if (selectedWorkerUI == null)
        //{
        //    Debug.LogError("Selected worker UI was null.");
        //    return;
        //}

        //workersController.AssignSoloWorker(selectedWorkerUI.GetWorker(), pContract);
        //DeselectWorker();
    }

    /// <summary>
    /// Makes sure only the next needed parking spot can be clicked on for an upgrade
    /// </summary>
    /// <param name="currentSmallParking"></param>
    /// <param name="currentLargeParking"></param>
    public void UpdateParkingUpgradeIcons(int currentParking)
    {
        for (int i = 0; i < upgradeParkings.Count; i++)
        {
            upgradeParkings[i].ToggleIconButton(false);

            if (i == currentParking)
            {
                upgradeParkings[i].ToggleIconButton(true);
            }
        }
    }
}
