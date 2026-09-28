using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Responsible for managing contracts and isuuing rewards and fines.
/// </summary>
public class ContractsController : MonoBehaviour
{
    private List<Contract> contracts = new List<Contract>();
    private List<Contract> acceptedContracts => new List<Contract>(contracts.Where(x => x.state == ContractState.Accepted || x.state == ContractState.InProgress));

    //Singleton 
    private static ContractsController _instance;

    public static ContractsController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<ContractsController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<ContractsController>().Length > 1)
                {
                    Debug.LogWarning("More than one ContractsController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("ContractsController");
                    _instance = singletonObject.AddComponent<ContractsController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of ContractsController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // If an instance already exists, destroy this GameObject
            Destroy(gameObject);
        }

        TimeController.Instance.OnDayPassed += HandleDayPassed;
    }

    private void OnDestroy()
    {
        foreach (var contract in contracts)
            contract.OnContractStateChange -= HandleContractStateChange;

        TimeController.Instance.OnDayPassed -= HandleDayPassed;
    }

    public void HandleLoad()
    {
        List<Contract> contracts = SaveController.Instance.GetData().Contracts;

        foreach (var contract in contracts)
        {
            LoadContract(contract);
        }
    }

    public void AddAvaiableContract(RoadDamage pRoadDamage)
    {
        //Add the contract info
        Contract contract = new Contract(pRoadDamage.reward, pRoadDamage, pRoadDamage.daysToFinish); //Default values like 100 reward and 10 min timer

        if (pRoadDamage == null)
            return;

        contract.SetContractState(ContractState.Available);
        //Add the contract UI listing
        UIManager.Instance.AddContractToList(contract);

        //Add to the avaiable contracts
        contracts.Add(contract);

        //Track the contracts progress
        contract.OnContractStateChange += HandleContractStateChange;

        ////Remove the road damage from availables 
        //foreach (RoadDamage takenRoadDamage in roadDamages)
        //    avaiableRoadDamages.Remove(takenRoadDamage);

        // Save
        SaveController.Instance.UpdateData(x => x.Contracts.Add(contract));
    }

    public void LoadContract(Contract pContract)
    {
        RoadDamage roadDamage = RoadsController.Instance.GetRoadDamage(pContract.roadDamageID);
        Contract contract = new Contract(pContract.reward, roadDamage, pContract.daysToFinish);

        contract.roadDamage = roadDamage;
        contract.roadDamageID = pContract.roadDamageID;
        contract.deadlineString = pContract.deadlineString;
        contract.progress = pContract.progress;

        // Add the contract UI listing
        UIManager.Instance.AddContractToList(contract);

        // Update the road damage
        roadDamage.SetAssignedContract(contract);
        roadDamage.LoadAllAssignedMaterialsAndTools();

        // Add to the avaiable contracts
        contracts.Add(contract);
        contract.OnContractStateChange += HandleContractStateChange;
        contract.SetContractState(pContract.state);
    }

    public void HandleContractStateChange(Contract contract)
    {
        if (contract == null)
        {
            contracts.Remove(contract);
            return;
        }

        switch (contract.state)
        {
            case ContractState.Available:
                break;
            case ContractState.Accepted:
                UIManager.Instance.AddContractManagerToList(contract);
                break;
            case ContractState.Completed:

                //Reward
                MoneyController.Instance.AddMoney(contract.reward, contract.roadDamage.damageName);

                RemoveContract(contract);
                break;
            case ContractState.Failed:
                MoneyController.Instance.PayoutMoney(contract.reward / 10, contract.roadDamage.damageName);
                //RemoveContract(contract);
                break;
            case ContractState.Canceled:
                RemoveContract(contract);
                break;
            default:
                break;
        }
    }

    public void HandleDayPassed(DateTime time)
    {
        for (int i = 0; i < acceptedContracts.Count; i++)
        {
            if (time.Day == acceptedContracts[i].deadline.Day)
            {
                acceptedContracts[i].SetContractState(ContractState.Failed);
                i--;
            }
        }
    }

    public bool DoesRoadDamageFitRequirements(int workersCount, InventoryInstanceCategory assignedTools, InventoryCategory assignedMaterials, RoadDamage roadDamage)
    {
        bool hasWorkerRequirement = workersCount >= roadDamage.minWorkers;
        bool hasToolsRequirement = true;
        bool hasMaterialRequirement = true;

        // Check tool requirements
        foreach (var toolRequirement in roadDamage.toolsRequirements)
        {
            int assignedToolCount = assignedTools.Instances
                .Count(i => i.ItemID == toolRequirement.Key);

            if (assignedToolCount < toolRequirement.Value)
            {
                hasToolsRequirement = false;
                break;
            }
        }

        // Check material requirements (still count-based)
        foreach (var materialRequirement in roadDamage.materialsRequirements)
        {
            if (!assignedMaterials.Items.TryGetValue(materialRequirement.Key, out int assignedAmount) ||
                assignedAmount < materialRequirement.Value)
            {
                hasMaterialRequirement = false;
                break;
            }
        }

        return hasWorkerRequirement && hasToolsRequirement && hasMaterialRequirement;
    }

    public void RemoveContract(Contract contract)
    {
        print("Removing Contract");
        // Update save
        SaveController.Instance.UpdateData(x => x.Contracts.RemoveAll(x => x.roadDamageID == contract.roadDamageID));
        contracts.Remove(contract);
    }

    public Contract GetContractByRoadDamageID(string roadDamageID)
    {
        return contracts.Find(x => x.roadDamageID == roadDamageID);
    }

}
