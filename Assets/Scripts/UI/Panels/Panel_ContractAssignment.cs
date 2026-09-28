using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Panel_ContractAssignment : Panel
{
    [Header("References")]
    [SerializeField] GameObject workerListingPrefab;
    [SerializeField] RectTransform workersContent;
    [SerializeField] TextMeshProUGUI txt_Title;
    [SerializeField] TextMeshProUGUI txt_Reward;
    [SerializeField] TextMeshProUGUI txt_Deadline;
    [SerializeField] TextMeshProUGUI txt_TimeToComplete;
    [SerializeField] TextMeshProUGUI txt_Workers;
    [SerializeField] List<UI_InventoryItem> tools;
    [SerializeField] List<UI_InventoryItem> materials;
    [SerializeField] Button btn_Confirm;

    [Header("Variables")]
    private List<WorkerListing_Assign> listedWorkers = new List<WorkerListing_Assign>();
    private List<WorkerListing_Assign> selectedWorkers = new List<WorkerListing_Assign>();
    private RoadDamage assignedRoadDamage;
    private Contract assignedContract;

    private void Awake()
    {
        OnPanelClosed += DeselectAllWokrers;
    }

    private void OnDestroy()
    {
        OnPanelClosed -= DeselectAllWokrers;
    }

    public void AddWorkerToList(Worker pWorker)
    {
        GameObject listingObj = Instantiate(workerListingPrefab, workersContent);
        WorkerListing_Assign workerListing = listingObj.GetComponent<WorkerListing_Assign>();
        workerListing.SetWorker(this, pWorker);
        listedWorkers.Add(workerListing);
    }

    public void SelectWorker(WorkerListing_Assign pWorkerUI)
    {
        pWorkerUI.ToggleSelect(true);
        //Worker worker = pWorkerUI.GetWorker();

        if (!selectedWorkers.Contains(pWorkerUI))
            selectedWorkers.Add(pWorkerUI);

        UpdateConfirmBtn();
        UpdateWorkersCount();
        UpdateAllListings();
    }

    public void DeselectWorker(WorkerListing_Assign pWorkerUI)
    {
        if (selectedWorkers.Contains(pWorkerUI)) selectedWorkers.Remove(pWorkerUI);

        pWorkerUI.DeselectToggle();
        pWorkerUI.ToggleSelect(false);
        UpdateConfirmBtn();
        UpdateWorkersCount();
        UpdateAllListings();
    }

    public void DeselectAllWokrers()
    {
        //Using for loop because we are modifying the list 
        for (int i = 0; i < selectedWorkers.Count;)
        {
            DeselectWorker(selectedWorkers[i]);
        }
        //foreach (WorkerListing_Assign workerUI in selectedWorkers)
        //{
        //    DeselectWorker(workerUI);
        //}
    }

    public void SetInfo(RoadDamage roadDamage, Contract contract)
    {
        assignedRoadDamage = roadDamage;
        assignedContract = contract;

        txt_Title.text = roadDamage.damageName;
        txt_Reward.text = contract.reward.ToString();
        txt_Deadline.text = contract.deadline.Date.ToString("dd/MM/yy");
        txt_Workers.text = $"{roadDamage.assignedWorkers.Count} / {roadDamage.minWorkers.ToString()}";

        //Update already assigned workers, for when openeing in progress road damage
        foreach (var assignedWorker in roadDamage.assignedWorkers)
        {
            foreach (var listedWorker in listedWorkers)
            {
                if(assignedWorker == listedWorker)
                {
                    SelectWorker(listedWorker);
                }
            }
        }      

        // --- Tools ---
        int i = 0;
        foreach (var pair in roadDamage.toolsRequirements)
        {
            if (i >= tools.Count)
            {
                Debug.LogError("Can't set tool info, out of range");
                break;
            }

            var itemUI = tools[i];
            itemUI.gameObject.SetActive(true);

            ItemData itemData = UIManager.Instance.GetInventoryItem(pair.Key);
            itemUI.SetName(itemData.Name);
            itemUI.SetIcon(itemData.Icon);
            itemUI.SetItemID(itemData.ItemID);

            int assignedAmount = roadDamage.tempAssignedEquipment.GetInstances(pair.Key).Count;
            itemUI.SetCount(pair.Value, assignedAmount); // required / tempAssigned

            i++;
        }

        for (; i < tools.Count; i++)
            tools[i].gameObject.SetActive(false);

        // --- Materials ---
        int j = 0;
        foreach (var pair in roadDamage.materialsRequirements)
        {
            if (j >= materials.Count)
            {
                Debug.LogError("Can't set material info, out of range");
                break;
            }

            var itemUI = materials[j];
            itemUI.gameObject.SetActive(true);

            ItemData itemData = UIManager.Instance.GetInventoryItem(pair.Key);
            itemUI.SetName(itemData.Name);
            itemUI.SetIcon(itemData.Icon);
            itemUI.SetItemID(itemData.ItemID);

            int assignedAmount = roadDamage.tempAssignedMaterials.GetAmount(pair.Key);
            itemUI.SetCount(pair.Value, assignedAmount); // required / tempAssigned

            j++;
        }

        for (; j < materials.Count; j++)
            materials[j].gameObject.SetActive(false);
    }



    private void UpdateConfirmBtn()
    {
        btn_Confirm.interactable = ContractsController.Instance.DoesRoadDamageFitRequirements(selectedWorkers.Count, assignedRoadDamage.tempAssignedEquipment, assignedRoadDamage.tempAssignedMaterials, assignedRoadDamage);
    }

    private void UpdateWorkersCount()
    {
        txt_Workers.text = $"{selectedWorkers.Count} / {assignedRoadDamage.minWorkers.ToString()}";
    }

    private void UpdateAllListings()
    {

        //Update the selectors so they do need exceed the max possible workers
        foreach (var worker in listedWorkers)
        {
            if (selectedWorkers.Contains(worker)) continue;

            worker.UpdateAssignmentAvaialability(selectedWorkers.Count < assignedRoadDamage.maxWorkers);
        }
    }


    #region Buttons

    public void Btn_Confirm()
    {
        //Extract the Workers reference from the lists and pass it to the workers controller
        assignedRoadDamage.ConfirmContract(selectedWorkers);
        UIManager.Instance.CloseCurrentPanel();
    }

    #endregion
}
