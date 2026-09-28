using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows;

public class InventoryController : MonoBehaviour
{
    [SerializeField] ItemsDatabase itemsDatabase;

    // Use the new EquipmentInventoryCategory for equipment
    public InventoryInstanceCategory Equipment { get; private set; } = new();
    //public InventoryCategory Equipment { get; private set; } = new();
    //public InventoryCategory AvailableTools { get; private set; } = new();
    public InventoryCategory Materials { get; private set; } = new();
    //public InventoryCategory AvailableMaterials { get; private set; } = new();


    //Singleton 
    private static InventoryController _instance;

    public static InventoryController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<InventoryController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<InventoryController>().Length > 1)
                {
                    Debug.LogWarning("More than one InventoryController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    //GameObject singletonObject = new GameObject("InventoryController");
                    //_instance = singletonObject.AddComponent<InventoryController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of InventoryController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // If an instance already exists, destroy this GameObject
            //Destroy(gameObject);
        }

        //Delay so others have time to subscribe to inventory changes
        //DelayRunner.Instance.Delay(0.2f, () => AssignMaterialsAndTools_Debug());
        //IntitAvaialbleInventory();
    }

    private void Start()
    {
        //Delay so others have time to subscribe to inventory changes
        //DelayRunner.Instance.Delay(0.2f, () => AssignMaterialsAndTools_Debug());
    }

    public void HandleLoad()
    {
        InventoryData inventoryData = SaveController.Instance.GetData().InventoryData;

        // Reconstruct Equipment
        Equipment.Instances = new List<ItemInstance>();

        foreach (var data in inventoryData.EquipmentInstances)
        {
            var def = itemsDatabase.GetItem<EquipmentItem>(data.ItemID);
            if (def == null)
            {
                Debug.LogWarning($"[InventoryController] Equipment definition not found for ID: {data.ItemID}");
                continue;
            }

            var instance = new EquipmentInstance(def)
            {
                InstanceID = data.InstanceID,
                IsRented = data.IsRented,
                CurrentPrice = data.CurrentPrice,
                ParkingId = data.ParkingID,
                IsAvailable = data.IsAvailable,

            };
            instance.SetCondition(data.CurrentCondition);

            Equipment.AddInstance(instance);

            // Spawn the transport
            ParkingSpot parking = CompanyController.Instance.GetParkingByID(data.ParkingID);
            GameObject spawnedTransportObj = TransportController.Instance.SpawnTransport(instance.TransportType, parking.GetParkingPosition().position);
            Transport spawnedTransport = spawnedTransportObj.GetComponent<Transport>();
            instance.Transport = spawnedTransport;

            // Assign the parking spot
            CompanyController.Instance.AssignVehicleToSpot(spawnedTransport, parking);
            CompanyController.Instance.ParkTransport(spawnedTransport);
        }

        // Reconstruct Materials
        Materials.Items = new Dictionary<string, int>();
        Materials.AvailableItems = new Dictionary<string, int>();

        foreach (var pair in inventoryData.MaterialsTotal)
        {
            Materials.AddItem(pair.Key, pair.Value);
        }

        //foreach (var pair in inventoryData.MaterialsTotal)
        //{
        //    if (!Materials.Items.ContainsKey(pair.Key))
        //    {
        //        Materials.Items[pair.Key] = pair.Value;
        //        Materials.OnNewItemAdded?.Invoke(pair.Key);
        //    }
        //}

        //foreach (var pair in inventoryData.MaterialsAvailable)
        //{
        //    if (!Materials.AvailableItems.ContainsKey(pair.Key))
        //        Materials.AvailableItems[pair.Key] = pair.Value;
        //}
    }

    public ItemCategory GetItemCategory(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("Item Category is null.");
            return ItemCategory.Null;
        }

        var parts = itemID.Split('_');
        if (parts.Length >= 2)
        {
            if ($"{parts[0]}_{parts[1]}" == "item_equipment")
            {
                return ItemCategory.Equipment;
            }
            else if ($"{parts[0]}_{parts[1]}" == "item_material")
            {
                return ItemCategory.Material;
            }
        }

        return ItemCategory.Null;
    }

    /// <summary>
    /// Updated to handle individual equipment instances
    /// </summary>
    public void AddAnyItems(Dictionary<string, int> items)
    {
        foreach (var item in items)
        {
            ItemCategory category = GetItemCategory(item.Key);

            switch (category)
            {
                case ItemCategory.Equipment:
                    //// Create individual equipment instances
                    //for (int i = 0; i < item.Value; i++)
                    //{
                    //    var equipmentInstance = itemsDatabase.CreateItemInstance(item.Key) as EquipmentInstance;
                    //    Equipment.AddInstance(equipmentInstance);
                    //}
                    break;
                case ItemCategory.Material:
                    // Keep materials as simple counts
                    Materials.AddItem(item.Key, item.Value);
                    break;
                default:
                    Debug.LogError("Category of adding item is null.");
                    continue;
            }
        }

        SaveData();
    }

    public void AddAnyInstance(ItemInstance itemInstance)
    {
        ItemCategory category = GetItemCategory(itemInstance.ItemID);

        // Assign unique ID
        itemInstance.InstanceID = itemInstance.ItemID + "_" + Equipment.Instances.Count;

        switch (category)
        {
            case ItemCategory.Equipment:
                Equipment.AddInstance(itemInstance);
                break;
            default:
                Debug.LogError("Category of adding item is null.");
                break;
        }

        SaveData();
    }

    public void ReturnTransport(Transport transport)
    {
        string equipmentInstanceID = GetTransportInstanceID(transport);
        Equipment.ReturnInstance(GetEquipmentByID(equipmentInstanceID));
    }

    public void BorrowTransport(Transport transport)
    {
        string equipmentInstanceID = GetTransportInstanceID(transport);
        Equipment.BorrowInstance(GetEquipmentByID(equipmentInstanceID));
    }

    /// <summary>
    /// Updated to handle both equipment instances and material counts
    /// </summary>
    public int GetValueOfcategory(InventoryCategory category)
    {
        float value = 0;

        foreach (var item in category.Items)
        {
            value += itemsDatabase.GetItem(item.Key).Price * item.Value;
        }

        return Mathf.RoundToInt(value);
    }

    /// <summary>
    /// Get total value of equipment instances
    /// </summary>
    public int GetInstancesValue(InventoryInstanceCategory category)
    {
        float value = 0;
        var allEquipmentTypes = category.Instances;

        foreach (var item in allEquipmentTypes)
        {
            value += item.CurrentPrice;
        }

        return Mathf.RoundToInt(value);
    }

    public EquipmentInstance GetEquipmentByID(string instanceID)
    {
        return (EquipmentInstance)Equipment.Instances.Find(x => x.InstanceID == instanceID);
    }

    public List<Transport> GetNeededTrucks(int workersCount)
    {
        List<Transport> selectedTrucks = new List<Transport>();
        Queue<Transport> availableTrucks = new Queue<Transport>();
        int workersLeftToFit = workersCount;

        foreach (ItemInstance item in Equipment.AvailableInstances)
        {
            EquipmentInstance equipment = item as EquipmentInstance;
            if (equipment != null)
            {
                if (equipment.TransportType == TransportType.Truck)
                    availableTrucks.Enqueue(equipment.Transport);
            }
            else
                Debug.LogError("Cast failed");
        }

        while (workersLeftToFit > 0)
        {
            if (availableTrucks.Count > 0)
            {
                Transport truck = availableTrucks.Dequeue();
                selectedTrucks.Add(truck);
                workersLeftToFit -= 4; // Up to 4 workers per truck
            }
            else
                break;
        }

        return selectedTrucks;
    }

    public string GetTransportInstanceID(Transport transport)
    {
        string ID = string.Empty;

        foreach (ItemInstance item in Equipment.Instances)
        {
            EquipmentInstance equipmentInstance = item as EquipmentInstance;
            if (equipmentInstance != null && equipmentInstance.Transport == transport)
            {
                ID = equipmentInstance.InstanceID;
                return ID;
            }
        }

        return ID;
    }

    public void MaintainEquipment(EquipmentInstance equipment)
    {
        int price = MoneyController.Instance.CalculateMaintenance(equipment);

        if (!MoneyController.Instance.CanAfford(price))
        {
            Debug.LogWarning("Cant afford maintainance");
            return;
        }

        MoneyController.Instance.SpendMoney(price, "Maintainance");
        equipment.SetCondition(100f);
    }

    public float CalculateWearToEquipment(EquipmentInstance equipment, float workProgressed)
    {
        const float wearPer18000 = 2f; // 2% = 2 condition loss per 18,000 progress points, approximately 20 min of gameplay
        float wearRate = wearPer18000 / 18000f; // Wear per progress point (in 0.0000011.. range)

        float wearFraction = workProgressed * wearRate * 1f; // Result in 0.0x format (e.g., 0.0005 for 0.05%) Multiply by 10 for testing, to make it faster
        return wearFraction;
    }


    public void ReturnEquipmentToMarket(EquipmentInstance equipment)
    {
        Transform marketDestination = MarketController.Instance.equipmentMarketEntry;
        Transport transport = equipment.Transport;
        TransportController.Instance.SendTransport(transport.transform.position, marketDestination.position, () => { }, transport, returnAfterArrival: false, destroyAfterReturn: true);
    }

    ///// <summary>
    ///// Entry point to other lists like Equipment and Materials. Simplifis the call and sorts the items automatically by category.
    ///// </summary>
    ///// <param name="items"></param>
    //public void AddAnyItems(Dictionary<string, int> items)
    //{
    //    foreach (var item in items)
    //    {
    //        ItemCategory category = GetItemCategory(item.Key);

    //        switch (category)
    //        {
    //            case ItemCategory.Equipment:
    //                Equipment.AddItem(item.Key, item.Value);
    //                break;
    //            case ItemCategory.Material:
    //                Materials.AddItem(item.Key, item.Value);
    //                break;
    //            default:
    //                Debug.LogError("Category of adding item is null.");
    //                continue;
    //        }
    //    }
    //}

    //public void IntitAvaialbleInventory()
    //{
    //    foreach (var item in AllTools.Items)
    //        AvailableTools.AddItem(item.Key, item.Value);

    //    foreach (var item in AllMaterials.Items)        
    //        AvailableMaterials.AddItem(item.Key, item.Value);        
    //}

    #region Saving

    public void SaveData()
    {
        var save = new InventoryData();

        // --- Materials ---
        // Total
        save.MaterialsTotal = new SerializableDictionary<string, int>(Materials.Items).Items;
        // Available
        save.MaterialsAvailable = new SerializableDictionary<string, int>(Materials.AvailableItems).Items;

        // --- Equipment Instances ---
        // All instances
        save.EquipmentInstances = Equipment.Instances
            .OfType<EquipmentInstance>()
            .Select(e => new EquipmentInstanceData(e))
            .ToList();

        SaveController.Instance.UpdateData(x => x.InventoryData = save);
    }


    /// <summary>
    /// Reconstitute inventory from a saved snapshot.
    /// </summary>
    public void LoadFromSaveData(InventoryData save)
    {
        // --- Materials ---
        Materials = new InventoryCategory();
        // Total
        var totalDict = new SerializableDictionary<string, int>();
        totalDict.Items = save.MaterialsTotal;
        Materials.Items = totalDict.ToDictionary();
        // Available
        var availDict = new SerializableDictionary<string, int>();
        availDict.Items = save.MaterialsAvailable;
        Materials.AvailableItems = availDict.ToDictionary();

        // --- Equipment Instances ---
        Equipment = new InventoryInstanceCategory();
        var allInst = new List<EquipmentInstance>();
        // 1) Recreate every instance
        foreach (var data in save.EquipmentInstances)
        {
            var inst = itemsDatabase
                .CreateItemInstance(data.ItemID) as EquipmentInstance;
            inst.SetCondition(data.CurrentCondition);
            inst.InstanceID = data.InstanceID;
            inst.CurrentPrice = data.CurrentPrice;
            allInst.Add(inst);
            Equipment.AddInstance(inst);
        }
    }
    #endregion
}

[System.Serializable]
public class InventoryCategory
{
    // Events to notify UI or other systems about inventory changes.
    // Event for a new item being added.
    public event Action<string> OnNewItemAdded;
    // Event for any update; parameters: key, current total, current available.
    public event Action<string, int, int> OnItemUpdated;

    // Total items count.
    public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
    // Subset of items available for use.
    public Dictionary<string, int> AvailableItems { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Adds an item to both total and available counts.
    /// </summary>
    public void AddItem(string key, int amount = 1)
    {
        if (!Items.ContainsKey(key))
        {
            Items[key] = 0;
            AvailableItems[key] = 0;
            OnNewItemAdded?.Invoke(key);
        }

        Items[key] += amount;
        AvailableItems[key] += amount;
        OnItemUpdated?.Invoke(key, Items[key], AvailableItems[key]);
    }

    /// <summary>
    /// Removes an item from the total count.
    /// Only removes available items if available equals total count.
    /// This is for cases where removal (consumption) only uses the items if
    /// they’re not already borrowed.
    /// </summary>
    public bool RemoveItem(string key, int amount = 1)
    {
        // Check if the item exists and there are enough total items.
        if (!Items.ContainsKey(key) || Items[key] < amount)
            return false;

        // If all items are available, remove from the available count.
        if (AvailableItems.TryGetValue(key, out int availableCount))
        {
            if (availableCount == Items[key])
            {
                AvailableItems[key] -= amount;
            }
            // Otherwise, assume some items have already been borrowed;
            // no change to available count needed.
        }

        Items[key] -= amount;
        if (Items[key] <= 0)
        {
            Items.Remove(key);
            AvailableItems.Remove(key);
        }

        OnItemUpdated?.Invoke(key, Items.ContainsKey(key) ? Items[key] : 0, AvailableItems.ContainsKey(key) ? AvailableItems[key] : 0);
        return true;
    }

    /// <summary>
    /// Borrows an item, i.e. reduces its available count.
    /// </summary>
    public bool BorrowItem(string key, int amount = 1)
    {
        if (!AvailableItems.ContainsKey(key) || AvailableItems[key] < amount)
            return false;
        AvailableItems[key] -= amount;
        OnItemUpdated?.Invoke(key, Items[key], AvailableItems[key]);
        return true;
    }

    /// <summary>
    /// Returns an item, increasing its available count but not exceeding the total.
    /// </summary>
    public bool ReturnItem(string key, int amount = 1)
    {
        if (!Items.ContainsKey(key))
            return false;
        int newAvailable = AvailableItems.ContainsKey(key) ? AvailableItems[key] + amount : amount;
        if (newAvailable > Items[key])
            newAvailable = Items[key];
        AvailableItems[key] = newAvailable;
        OnItemUpdated?.Invoke(key, Items[key], AvailableItems[key]);
        return true;
    }

    /// <summary>
    /// Utility: Get total count of an item.
    /// </summary>
    public int GetAmount(string key) => Items.TryGetValue(key, out int count) ? count : 0;

    /// <summary>
    /// Utility: Get available count of an item.
    /// </summary>
    public int GetAvailableAmount(string key) => AvailableItems.TryGetValue(key, out int count) ? count : 0;
}

[System.Serializable]
public class InventoryInstanceCategory
{
    public event Action<ItemInstance> OnNewInstanceAdded;

    public List<ItemInstance> Instances { get; set; } = new();
    public List<ItemInstance> AvailableInstances => new List<ItemInstance>(Instances.Where(x => x.IsAvailable));

    /// <summary>
    /// Adds a new item instance to inventory.
    /// </summary>
    public void AddInstance(ItemInstance instance)
    {
        if (!Instances.Contains(instance))
        {
            Instances.Add(instance);
            OnNewInstanceAdded?.Invoke(instance);
        }
    }

    /// <summary>
    /// Removes an instance from the inventory.
    /// </summary>
    public bool RemoveInstance(ItemInstance instance)
    {
        bool removed = Instances.Remove(instance);

        return removed;
    }

    /// <summary>
    /// Marks a specific instance as in-use (borrowed).
    /// </summary>
    public bool BorrowInstance(ItemInstance instance)
    {
        instance.IsAvailable = false;
        instance.OnInstanceUpdated?.Invoke(instance);
        return true;
    }

    /// <summary>
    /// Returns an instance back to availability.
    /// </summary>
    public bool ReturnInstance(ItemInstance instance)
    {
        instance.IsAvailable = true;
        instance.OnInstanceUpdated?.Invoke(instance);
        return true;
    }

    /// <summary>
    /// Get all instances with a specific itemID.
    /// </summary>
    public List<ItemInstance> GetInstances(string itemID)
    {
        return Instances
            .Where(i => i.ItemID == itemID)
            .ToList();
    }

    /// <summary>
    /// Get all available instances with a specific itemID.
    /// </summary>
    public List<ItemInstance> GetAvailableInstances(string itemID)
    {
        return AvailableInstances
            .Where(i => i.ItemID == itemID)
            .ToList();
    }

    public ItemInstance GetInstanceByID(string instanceID)
    {
        return Instances.Find(x => x.InstanceID == instanceID);
    }
}


public enum ItemCategory
{
    Null,
    Equipment,
    Material
}



[Serializable]
public class InventoryData
{
    // Materials
    public List<SerializableKeyValuePair<string, int>> MaterialsTotal = new();
    public List<SerializableKeyValuePair<string, int>> MaterialsAvailable = new();

    // Equipment instances
    public List<EquipmentInstanceData> EquipmentInstances = new();
}


[Serializable]
public class SerializableKeyValuePair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public SerializableKeyValuePair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    public List<SerializableKeyValuePair<TKey, TValue>> Items = new();

    public SerializableDictionary() { }

    public SerializableDictionary(Dictionary<TKey, TValue> dict)
    {
        foreach (var kvp in dict)
        {
            Items.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
        }
    }

    public Dictionary<TKey, TValue> ToDictionary()
    {
        return Items.ToDictionary(item => item.Key, item => item.Value);
    }
}
