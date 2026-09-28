using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemsDatabase", menuName = "Custom/ItemsDatabase")]
public class ItemsDatabase : ScriptableObject
{
    [Header("Materials")]
    public List<MaterialItem> materialItems = new();

    [Header("Equipment")]
    public List<EquipmentItem> equipmentItems = new();

    // Combined at runtime
    private Dictionary<string, ItemData> itemLookup;

    private void OnEnable()
    {
        BuildItemLookup();
    }

    private void BuildItemLookup()
    {
        itemLookup = new Dictionary<string, ItemData>();

        foreach (var item in materialItems)
            itemLookup[item.ItemID] = item;

        foreach (var item in equipmentItems)
            itemLookup[item.ItemID] = item;

    }

    public ItemData GetItem(string itemID)
    {
        if (itemLookup == null)
            BuildItemLookup();

        itemLookup.TryGetValue(itemID, out var item);

        if (item == null)
            Debug.LogError($"Trying to get Item, but its null: {itemID}");

        return item;
    }

    public T GetItem<T>(string itemID) where T : ItemData
    {
        return GetItem(itemID) as T;
    }

    public List<ItemData> GetAllItems()
    {
        return itemLookup?.Values.ToList() ?? new List<ItemData>();
    }

    public ItemInstance CreateItemInstance(string itemID)
    {
        var def = GetItem(itemID);
        if (def is EquipmentItem equipDef)
            return new EquipmentInstance(equipDef);
        else
            return new ItemInstance(def);
    }
}

[Serializable]
public class ItemData
{
    public string ItemID;
    public string Name;
    public Sprite Icon;
    public int Price;
}

[Serializable]
public class MaterialItem : ItemData
{

}

[Serializable]
public class EquipmentItem : ItemData
{
    public TransportType TransportType;
    public float BaseCondition = 100f;
}

// Instances 
[Serializable]
public class ItemInstance
{
    protected ItemData Definition { get; }

    public ItemInstance(ItemData definition)
    {
        Definition = definition;
        CurrentPrice = definition.Price;
    }

    public Action<ItemInstance> OnInstanceUpdated { get; set; }

    public string ItemID => Definition.ItemID;

    public string Name => Definition.Name;
    public Sprite Icon => Definition.Icon;

    public string InstanceID;
    public int CurrentPrice { get; set; }

    public bool IsAvailable = true;
}

[Serializable]
public class EquipmentInstance : ItemInstance
{
    public EquipmentInstance(EquipmentItem def) : base(def)
    {
        CurrentCondition = def.BaseCondition;
    }

    public Transport Transport { get; set; }
    public TransportType TransportType => ((EquipmentItem)Definition).TransportType;
    public float CurrentCondition { get; private set; }

    public bool IsRented = false;
    public int ParkingId = -1;

    public EquipmentInstance Clone()
    {
        var def = (EquipmentItem)this.Definition;
        var clone = new EquipmentInstance(def)
        {
            CurrentCondition = this.CurrentCondition,
            CurrentPrice = this.CurrentPrice
        };
        return clone;
    }

    public void SetCondition(float condition)
    {
        int roundedConditionBefore = Mathf.RoundToInt(CurrentCondition);

        CurrentCondition = condition;
        if(condition <= 0)
        {
            CurrentCondition = 0;
            InventoryController.Instance.Equipment.BorrowInstance(this); // Make it unavailable for work
        }

        int roundedConditionAfter = Mathf.RoundToInt(CurrentCondition);

        if (roundedConditionBefore == 0 && roundedConditionAfter > 0)        
            InventoryController.Instance.Equipment.ReturnInstance(this); // Make it available for work again
        
        if (roundedConditionBefore != roundedConditionAfter)
        {
            OnInstanceUpdated?.Invoke(this);
        }
    }
}

[Serializable]
public class EquipmentInstanceData
{
    public string ItemID;
    public string InstanceID;
    public float CurrentCondition;
    public int CurrentPrice;
    public bool IsRented;
    public int ParkingID;
    public bool IsAvailable;

    public EquipmentInstanceData() { }  // for JSON

    public EquipmentInstanceData(EquipmentInstance e)
    {
        ItemID = e.ItemID;
        InstanceID = e.InstanceID;
        CurrentCondition = e.CurrentCondition;
        CurrentPrice = e.CurrentPrice;
        IsRented = e.IsRented;
        ParkingID = e.ParkingId;
        IsAvailable = e.IsAvailable;
    }
}
