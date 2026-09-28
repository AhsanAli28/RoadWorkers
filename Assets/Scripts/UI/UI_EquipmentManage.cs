using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EquipmentManage : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI Txt_name;
    [SerializeField] TextMeshProUGUI Txt_price;
    [SerializeField] TextMeshProUGUI Txt_maintenancePrice;
    [SerializeField] TextMeshProUGUI Txt_condition;
    [SerializeField] GameObject rentedIcon;
    [SerializeField] GameObject returnConfirmation;
    [SerializeField] Button btn_maintenance;
    [SerializeField] Button btn_return;

    private EquipmentInstance equipmentInstance;
    private UI_Inventory inventoryUI;

    public void SetPanel(UI_Inventory inventory, Panel_Office officePanel)
    {
        inventoryUI = inventory;
        officePanel.OnPanelClosed += Btn_Close;
    }

    public void SetInstance(EquipmentInstance instance)
    {
        equipmentInstance = instance;
        UpdateAllInfo();
    }

    public void Btn_Close()
    {
        inventoryUI.ToggleEquipmentManageUI(false);
        ToggleReturnConfirmation(false);
    }

    public void Btn_Maintain()
    {
        InventoryController.Instance.MaintainEquipment(equipmentInstance);
        UpdateAllInfo();
    }

    public void Btn_ReturnEquipment()
    {
        ToggleReturnConfirmation(true);
    }

    public void Btn_ConfirmReturnEquipment()
    {
        ToggleReturnConfirmation(false);
        inventoryUI.ToggleEquipmentManageUI(false);

        inventoryUI.RemoveEquipmentUI(equipmentInstance);
        InventoryController.Instance.Equipment.RemoveInstance(equipmentInstance);
        InventoryController.Instance.ReturnEquipmentToMarket(equipmentInstance);
    }

    /// <summary>
    /// According the assigned instance
    /// </summary>
    public void UpdateAllInfo()
    {
        if (equipmentInstance == null)
        {
            Debug.LogError("Instance is null.");
            return;
        }

        int maintenancePrice = MoneyController.Instance.CalculateMaintenance(equipmentInstance);
        SetName(equipmentInstance.Name);
        SetIcon(equipmentInstance.Icon);
        SetPrice(equipmentInstance.CurrentPrice, equipmentInstance.IsRented);
        SetCondition(equipmentInstance.CurrentCondition);
        SetMaintenancePrice(maintenancePrice);
        ToggleRentedIcon(equipmentInstance.IsRented);
        UpdateMaintenanceButton(MoneyController.Instance.CanAfford(maintenancePrice) && equipmentInstance.CurrentCondition < 99f && !equipmentInstance.IsRented);
        ToggleReturnButton(equipmentInstance.IsRented);
    }

    private void SetName(string name)
    {
        Txt_name.text = name;
    }

    private void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    private void SetPrice(int price, bool isRent)
    {
        if (!isRent)
        {
            Txt_price.gameObject.SetActive(false);
        }
        else
        {
            Txt_price.gameObject.SetActive(true);
            Txt_price.text = $"Weekly rent:  ${MoneyController.Instance.CalculateEquipmentRent(price).ToString("N0")}";
        }
    }

    private void SetCondition(float condition)
    {
        Txt_condition.text = $"Condition: %{condition.ToString("F0")}";
    }

    private void SetMaintenancePrice(int price)
    {
        Txt_maintenancePrice.text = $"Price: ${price.ToString("N0")}";
    }


    private void ToggleRentedIcon(bool isRented)
    {
        rentedIcon.SetActive(isRented);
    }

    private void UpdateMaintenanceButton(bool value)
    {
        btn_maintenance.interactable = value;
    }

    public void ToggleReturnConfirmation(bool value)
    {
        returnConfirmation.SetActive(value);
    }

    public void ToggleReturnButton(bool value)
    {
        btn_return.gameObject.SetActive(value);
    }

}
