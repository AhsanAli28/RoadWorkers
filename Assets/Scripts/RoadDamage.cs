using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class RoadDamage : MonoBehaviour
{
    [Header("Parameters")]
    public string damageName;
    public int minWorkers;
    public int maxWorkers;
    [SerializeField] List<StringIntPair> toolsRequirementsSerialization = new List<StringIntPair>(); //String is name of the tool and int is the count
    [SerializeField] List<StringIntPair> materialsRequirementsSerialization = new List<StringIntPair>(); //String is name of the tool and int is the count
    public float progressRequirement;
    public int reward;
    public int daysToFinish;
    public RoadDamageType type;

    [Header("References")]
    public Slider progressBar;
    public Transform destinationPoint;
    public Transform pedestrianDestinationPoint;
    public GameObject model;
    [SerializeField] List<WorkingPointAnim> workingPoints;
    [SerializeField] GameObject progressBarParent;
    [SerializeField] GameObject constructionSite;
    [SerializeField] Popup_AcceptContract popup_AcceptContract;
    [SerializeField] Image icon;
    [SerializeField] Sprite assignIcon;
    [SerializeField] SpecialEffect specialEffect;
    [SerializeField] AudioSource soundEffect;

    [Header("Animations")]
    [SerializeField] Animator equipmentAnimator;
    [SerializeField] string animationClipName;
    [SerializeField] bool loopAnimation = false;
    [SerializeField] List<EquipmentIDChecker> equipmentVisuals;

    [Header("Variables")]
    public string RoadDamageID;
    public bool isConstructing;
    public bool isReadyToStart => arrivedWorkers.Count == minWorkers;
    public float progress; // Non-linear, baseline 15 points of progress per second
    private RoadDamageData roadDamageData;
    public List<Worker> assignedWorkers = new List<Worker>();
    public List<Worker> arrivedWorkers => new List<Worker>(assignedWorkers.Where(x => x.arrivedToWorkFlag));
    public Dictionary<string, int> materialsRequirements = new Dictionary<string, int>();
    public Dictionary<string, int> toolsRequirements = new Dictionary<string, int>();
    public InventoryInstanceCategory tempAssignedEquipment = new InventoryInstanceCategory();
    public InventoryCategory tempAssignedMaterials = new InventoryCategory();
    public InventoryInstanceCategory assignedEquipment = new InventoryInstanceCategory();
    public InventoryCategory assignedMaterials = new InventoryCategory();
    private Contract assignedContract;
    private Road parentRoad;
    private RoadSection parentRoadSection;
    private int workerPointCount;

    private void Awake()
    {
        //Init the dictionary for the tools
        foreach (var pair in toolsRequirementsSerialization)
            if (!toolsRequirements.ContainsKey(pair.key))
                toolsRequirements.Add(pair.key, pair.value);

        //Init the dictionary for materials
        foreach (var pair in materialsRequirementsSerialization)
            if (!materialsRequirements.ContainsKey(pair.key))
                materialsRequirements.Add(pair.key, pair.value);

        SetUIInfo();
    }

    public void BeginConstruction()
    {
        isConstructing = true;
        ToggleConstructionSite(true);

        if (loopAnimation)
            PlayAnimationLoop();
        if (damageName == "Road Reconstruction")
            parentRoad.SetupRoadReconstruction();
        SaveData();
        soundEffect.gameObject.SetActive(true);
    }


    public void CompleteConstruction()
    {
        isConstructing = false;
        ToggleConstructionSite(false);
        ToggleProgressBar(false);
        UpdateIcon();
        DeleteSavedData();
        progressBar.value = 0;
        ReturnInventory();
        parentRoad.FixRoadSection(parentRoadSection);
        if (specialEffect)
            specialEffect.PlayEffect();
    }

    public void CancelConstruction()
    {
        isConstructing = false;
        ToggleConstructionSite(false);
        ToggleProgressBar(false);
        UpdateIcon();
        DeleteSavedData();
        progressBar.value = 0;
        ReturnInventory();
    }

    public void ProgressWork(float pValue)
    {
        progress += pValue;
        progressBar.value = Mathf.InverseLerp(0, progressRequirement, progress);

        if (roadDamageData != null)
            roadDamageData.Progress = progress;

        if (!loopAnimation)
            UpdateEquipmentAnimationProgress(Mathf.InverseLerp(0, progressRequirement, progress));
    }

    public WorkingPointAnim AssignWorkPoint()
    {
        workerPointCount++;

        if (workerPointCount > workingPoints.Count) // When ran out of points, return null
            return null;
        else
            return workingPoints[workerPointCount - 1];
    }

    public void EnableEquipemnt(string itemID)
    {
        EquipmentIDChecker equipmentVisual = equipmentVisuals.Find(x => x.isThisItem(itemID));
        if (equipmentVisual != null)
        {
            print("EnableEquipment");
            equipmentVisual.gameObject.SetActive(true);
        }
    }

    public void ToggleConstructionSite(bool value)
    {
        constructionSite.SetActive(value);
    }

    public void ToggleProgressBar(bool pValue)
    {
        progressBarParent.SetActive(pValue);
    }

    public bool IsCompleted()
    {
        return progress >= progressRequirement;
    }

    #region UI

    public void ShowRelevantUI()
    {
        switch (assignedContract.state)
        {
            case ContractState.Available:
                if (popup_AcceptContract.isAnimating) break;

                if (popup_AcceptContract.isOpen)
                    popup_AcceptContract.Pop_Out();
                else
                    popup_AcceptContract.Pop_In();

                break;
            case ContractState.Accepted:
                //First automatically assign all the required tools and materials if you can 
                AssignTempRequirements();
                UIManager.Instance.SetContractAssignmentInfo(this, assignedContract);
                UIManager.Instance.ShowContractAssignment();
                break;
            case ContractState.InProgress:
                //UIManager.Instance.SetContractAssignmentInfo(this, assignedContract);
                //UIManager.Instance.ShowContractAssignment();
                break;
            case ContractState.Completed:
                break;
            case ContractState.Failed:
                AssignTempRequirements();
                UIManager.Instance.SetContractAssignmentInfo(this, assignedContract);
                UIManager.Instance.ShowContractAssignment();
                break;
            default:
                break;
        }
    }

    private void SetUIInfo()
    {
        if (popup_AcceptContract)
        {
            popup_AcceptContract.Txt_Title.text = damageName;
            popup_AcceptContract.Txt_Reward.text = reward.ToString();
            popup_AcceptContract.Txt_Workers.text = minWorkers.ToString();
            popup_AcceptContract.Txt_Tools.text = toolsRequirements.Count.ToString();
            popup_AcceptContract.SetMaterialsInfo(materialsRequirements);
        }
    }

    public void AssignTempRequirements()
    {
        tempAssignedEquipment = new InventoryInstanceCategory();
        tempAssignedMaterials = new InventoryCategory();

        var availableEquipment = InventoryController.Instance.Equipment; // This is InstanceInventoryCategory
        var availableMaterials = InventoryController.Instance.Materials.AvailableItems;

        // Assign tools (instances) as much as possible
        foreach (var req in toolsRequirements)
        {
            string itemID = req.Key;
            int requiredAmount = req.Value;

            // Get available instances of this tool
            List<ItemInstance> availableInstances = availableEquipment
                .GetAvailableInstances(itemID)
                .OfType<EquipmentInstance>()
                .OrderByDescending(e => e.CurrentCondition)
                .Cast<ItemInstance>() // Needed to match the AddInstance type
                .ToList();

            int toAssign = Mathf.Min(requiredAmount, availableInstances.Count);

            // Get equipment from the top of the sorted by condition list 
            for (int i = 0; i < toAssign; i++)
            {
                var instance = availableInstances[i];
                // Add to temp
                tempAssignedEquipment.AddInstance(instance);
            }
        }

        // Assign materials (count-based)
        foreach (var req in materialsRequirements)
        {
            int available = availableMaterials.TryGetValue(req.Key, out var val) ? val : 0;
            int toAssign = Mathf.Min(req.Value, available);
            if (toAssign > 0)
            {
                tempAssignedMaterials.AddItem(req.Key, toAssign);
            }
        }
    }


    public void ConfirmContract(List<WorkerListing_Assign> selectedWorkers)
    {
        // Extract workers from the UI
        List<Worker> workers = new List<Worker>();
        foreach (WorkerListing_Assign workerUI in selectedWorkers)
            workers.Add(workerUI.GetWorker());

        // Actually borrow tool instances and finalize assignment
        foreach (var instance in tempAssignedEquipment.Instances)
        {
            // This is redundant if you already borrowed it during assignment,
            // but you can re-ensure it's not available globally
            InventoryController.Instance.Equipment.BorrowInstance(instance);

            assignedEquipment.AddInstance(instance);
        }

        // Borrow material quantities
        foreach (var item in tempAssignedMaterials.Items)
        {
            InventoryController.Instance.Materials.BorrowItem(item.Key, item.Value);
            assignedMaterials.AddItem(item.Key, item.Value);
        }

        WorkersController.Instance.AssignWorkers(workers, assignedContract);

        if (roadDamageData != null)
            roadDamageData.AssignedEquipment = SerializeAssignedEquipment();
        else
            SaveData();
    }

    public void LoadAllAssignedMaterialsAndTools()
    {
        if (assignedContract != null && assignedContract.state == ContractState.Accepted || assignedContract.state == ContractState.InProgress)
        {
            assignedEquipment.Instances = new List<ItemInstance>(DeSerializeAssignedEquipment(roadDamageData.AssignedEquipment));
            // Assign material quantities
            foreach (var item in materialsRequirements)
            {
                assignedMaterials.AddItem(item.Key, item.Value);
            }
        }
    }

    public void ReturnInventory()
    {
        // Wear down borrowed tools
        foreach (var tool in assignedEquipment.Instances)
        {
            //InventoryController.Instance.Equipment.ReturnInstance(tool);
            EquipmentInstance equipment = (EquipmentInstance)tool;
            float wear = InventoryController.Instance.CalculateWearToEquipment(equipment, progress);
            equipment.SetCondition(equipment.CurrentCondition - wear);
        }

        // Remove used materials from total inventory
        foreach (var material in assignedMaterials.Items)
        {
            InventoryController.Instance.Materials.RemoveItem(material.Key, material.Value);
        }

        // Clear assigned after return
        assignedEquipment = new InventoryInstanceCategory();
        assignedMaterials = new InventoryCategory();
    }

    public void UpdateIcon()
    {
        switch (assignedContract.state)
        {
            case ContractState.Available:
                break;
            case ContractState.Accepted:
                icon.sprite = assignIcon;
                break;
            case ContractState.InProgress:
                icon.gameObject.SetActive(false);
                break;
            case ContractState.Completed:
                icon.gameObject.SetActive(false);
                break;
            case ContractState.Failed:
                //icon.gameObject.SetActive(false);
                break;
            case ContractState.Canceled:
                icon.gameObject.SetActive(false);
                break;
            default:
                break;
        }
    }

    #endregion

    #region Buttons

    public void Btn_Accept()
    {
        print($"AssignedContract: {assignedContract}");
        assignedContract.AcceptContract();
        popup_AcceptContract.Pop_Out();
    }


    public void Btn_Debug()
    {
        print("Button press");
    }

    #endregion

    public void SetAssignedContract(Contract contract)
    {
        assignedContract = contract;
        assignedContract.OnContractStateChange += HandleContractStateChange;
    }

    public void SetParentRoad(Road road, RoadSection section)
    {
        parentRoad = road;
        parentRoadSection = section;
    }

    public void AssignRoadDamageID(string pRoadDamageID)
    {
        RoadDamageID = pRoadDamageID;
        //SaveData();
    }

    public Contract GetAssignedContract()
    {
        return assignedContract;
    }

    public void AssignWorker(Worker worker)
    {
        if (!assignedWorkers.Contains(worker))
            assignedWorkers.Add(worker);
    }

    private void OnDestroy()
    {
        if (assignedContract != null)
            assignedContract.OnContractStateChange -= HandleContractStateChange;
    }

    private void HandleContractStateChange(Contract contract)
    {
        if (contract == null)
        {
            CancelConstruction();
            return;
        }

        UpdateIcon();

        if (contract.state == ContractState.Completed)
        {
            CompleteConstruction();
        }

        if (contract.state == ContractState.Failed)
        {
            //CancelConstruction();

            //Repair the road if road reconstruction was failed or cancelled. 
            //This is to keeo the game loop intact
            //TODO: Mange canceled and failed contracts better, like fixing them with time by competing companies 
            //if (damageName == "Road Reconstruction")
            //{
            //    parentRoad.RepairRoad();
            //}
        }

        if (contract.state == ContractState.Canceled)
        {
            CancelConstruction();

            ////Same as in Failed
            //if (damageName == "Road Reconstruction")
            //{
            //    parentRoad.RepairRoad();
            //}
        }
    }

    public bool IsEquipmentPartOfConstruction(string itemID)
    {
        return toolsRequirements.Any(x => x.Key == itemID);
    }

    #region Saving 

    public void SaveData()
    {
        if (roadDamageData == null)
        {
            roadDamageData = new RoadDamageData
            {
                RoadDamageID = RoadDamageID,
                Type = type,
                Progress = progress,
                IsConstructing = isConstructing,
                ParentRoadID = parentRoad.RoadID,
                RoadSectionID = parentRoadSection.RoadSectionID,
                AssignedEquipment = SerializeAssignedEquipment()
            };

            // Add it to GameData
            SaveController.Instance.UpdateData(gameData =>
            {
                gameData.RoadDamages.Add(roadDamageData);
            });
        }
        else
        {
            // Just update the reference directly
            roadDamageData.Type = type;
            roadDamageData.Progress = progress;
            roadDamageData.IsConstructing = isConstructing;
            roadDamageData.ParentRoadID = parentRoad.RoadID;
            roadDamageData.RoadSectionID = parentRoadSection.RoadSectionID;
            roadDamageData.AssignedEquipment = SerializeAssignedEquipment();
        }
    }


    public void DeleteSavedData()
    {
        SaveController.Instance.UpdateData(gameData =>
        {
            gameData.RoadDamages.RemoveAll(x => x.RoadDamageID == this.RoadDamageID);
        });

        roadDamageData = null;
    }

    private List<string> SerializeAssignedEquipment()
    {
        List<string> equipmentIDs = new List<string>();
        foreach (ItemInstance item in assignedEquipment.Instances)
        {
            equipmentIDs.Add(item.InstanceID);
        }

        return equipmentIDs;
    }

    private List<ItemInstance> DeSerializeAssignedEquipment(List<string> equipmentIDs)
    {
        List<ItemInstance> equipment = new List<ItemInstance>();

        foreach (string itemID in equipmentIDs)
        {
            equipment.Add(InventoryController.Instance.GetEquipmentByID(itemID));
        }

        return equipment;
    }

    public void SetData(RoadDamageData data)
    {
        roadDamageData = data;
    }

    #endregion

    #region Animation

    void PlayAnimationLoop()
    {
        if (equipmentAnimator != null)
        {
            equipmentAnimator.enabled = true;
            equipmentAnimator.Play(animationClipName, 0);
            equipmentAnimator.speed = 3f; 
        }
    }

    void UpdateEquipmentAnimationProgress(float progress)
    {
        if (equipmentAnimator != null)
        {
            equipmentAnimator.enabled = true;
            equipmentAnimator.Play(animationClipName, 0, progress);
            equipmentAnimator.speed = 0f; // Freeze it at that point
        }
    }

    #endregion
}

[System.Serializable]
public class StringIntPair
{
    public string key;
    public int value;
}

[Serializable]
public class RoadDamageData
{
    public string RoadDamageID;
    public RoadDamageType Type;
    public float Progress;
    public bool IsConstructing;
    public string ParentRoadID;
    public string RoadSectionID;
    public List<string> AssignedEquipment = new List<string>();
}

public enum RoadDamageType
{
    Cracks,
    Pothole,
    PipeLaying,
    RoadReconstruction
}
