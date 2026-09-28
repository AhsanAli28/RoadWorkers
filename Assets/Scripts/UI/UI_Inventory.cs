using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    [SerializeField] GameObject inventoryItemUIPrefab;
    [SerializeField] GameObject inventoryEquipmentUIPrefab;
    [SerializeField] Panel_Office officePanel;
    [SerializeField] Transform toolsContainer;
    [SerializeField] Transform materialsContainer;
    [SerializeField] UI_EquipmentManage equipmentManagePanel;

    private List<UI_InventoryEquipment> addedEquipment = new List<UI_InventoryEquipment>();

    public void Awake()
    {
        InventoryController.Instance.Equipment.OnNewInstanceAdded += SpawnEquipmentUI;
        InventoryController.Instance.Materials.OnNewItemAdded += SpawnMaterialUI;
    }

    private void OnDestroy()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.Equipment.OnNewInstanceAdded -= SpawnEquipmentUI;
            InventoryController.Instance.Materials.OnNewItemAdded -= SpawnMaterialUI;
        }

    }

    private void SpawnEquipmentUI(ItemInstance itemInstance)
    {
        EquipmentInstance equipmentInstance = (EquipmentInstance)itemInstance;
        if (equipmentInstance == null)
        {
            Debug.LogError("Cast failed.");
            return;
        }

        UI_InventoryEquipment equipmentUI = SpawnEquipmentUI(equipmentInstance, toolsContainer, InventoryController.Instance.Equipment);
        equipmentUI.SetCondition(equipmentInstance.CurrentCondition);
        equipmentUI.ToggleRent(equipmentInstance.IsRented);
    }

    private void SpawnMaterialUI(string itemID)
    {
        UI_InventoryItem item = SpawnItemUI(itemID, materialsContainer, InventoryController.Instance.Materials);
        item.DisplayAvaiables = false;
    }

    private UI_InventoryItem SpawnItemUI(string itemID, Transform parent, InventoryCategory category)
    {
        GameObject itemUI = Instantiate(inventoryItemUIPrefab, parent);
        UI_InventoryItem item = itemUI.GetComponent<UI_InventoryItem>();
        ItemData itemData = UIManager.Instance.GetInventoryItem(itemID);
        item.SetName(itemData.Name);
        item.SetItemID(itemData.ItemID);
        item.SetIcon(itemData.Icon);
        item.SetCount(category.GetAmount(itemID), category.GetAvailableAmount(itemID));
        item.SubscribeToInventoryCategory(category);
        return item;
    }

    private UI_InventoryEquipment SpawnEquipmentUI(ItemInstance instance, Transform parent, InventoryInstanceCategory category)
    {
        GameObject itemObj = Instantiate(inventoryEquipmentUIPrefab, parent);
        UI_InventoryEquipment item = itemObj.GetComponent<UI_InventoryEquipment>();

        item.SetPanel(this);
        item.SetName(instance.Name);
        item.SetItemID(instance.ItemID);
        item.SetInstance(instance);
        item.SetIcon(instance.Icon);
        item.SubscribeToInventoryCategory(category);
        addedEquipment.Add(item);

        return item;
    }

    public void RemoveEquipmentUI(EquipmentInstance instance)
    {
        UI_InventoryEquipment equipment = GetEquipmentUI(instance);
        if (addedEquipment.Contains(equipment))
            addedEquipment.Remove(equipment);
        Destroy(equipment.gameObject);
    }

    public void SetEquipmentManageInfo(EquipmentInstance instance)
    {
        equipmentManagePanel.SetPanel(this, officePanel);
        equipmentManagePanel.SetInstance(instance);
    }

    public void ToggleEquipmentManageUI(bool value)
    {
        equipmentManagePanel.gameObject.SetActive(value);
    }

    public UI_InventoryEquipment GetEquipmentUI(EquipmentInstance instance)
    {
        return addedEquipment.FirstOrDefault(x => x.GetEquipmentInstance() == instance);
    }

}
