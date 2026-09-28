using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InventoryEquipment : UI_InventoryInstanceItem
{
    [SerializeField] TextMeshProUGUI Txt_condition;
    [SerializeField] GameObject rentIcon;
    [SerializeField] Button btn_manage;

    public void SetCondition(float condition)
    {
        Txt_condition.text = $"{condition.ToString("F0")}%";
    }

    public void ToggleRent(bool value)
    {
        rentIcon.SetActive(value);
    }

    protected override void HandleInstanceUpdate(ItemInstance instance)
    {
        if (assignedInstance != instance) return;
        // If its equipment, update its condition 
        EquipmentInstance equipment = (EquipmentInstance)instance;
        if (equipment != null)
        {
            SetCondition(equipment.CurrentCondition);
        }

        UpdateManageButton();
    }

    public void Btn_Manage()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI is null");
            return;
        }

        EquipmentInstance equipmentInstance = (EquipmentInstance)assignedInstance;
        if (equipmentInstance == null)
        {
            Debug.LogError("Equipment instance is null");
            return;
        }

        inventoryUI.SetEquipmentManageInfo(equipmentInstance);
        inventoryUI.ToggleEquipmentManageUI(true);
    }

    public EquipmentInstance GetEquipmentInstance()
    {
        return (EquipmentInstance)assignedInstance;
    }

    private void UpdateManageButton()
    {
        EquipmentInstance equipment = (EquipmentInstance)assignedInstance;
        bool isBrokern = equipment.CurrentCondition <= 0;

        if (isBrokern)
        {
            btn_manage.interactable = true;
        }
        else 
        {
            btn_manage.interactable = equipment.IsAvailable;
        }
    }
}
