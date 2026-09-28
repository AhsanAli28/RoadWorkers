using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ConfirmEquipmentOrder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI Txt_name;
    [SerializeField] TextMeshProUGUI Txt_price;
    [SerializeField] TextMeshProUGUI Txt_condition;
    [SerializeField] TextMeshProUGUI txt_OrderType;
    [SerializeField] TextMeshProUGUI txt_DeliveryPrice;
    [SerializeField] Button btn_Confirm;

    private EquipmentInstance equipmentInstance;
    private Panel_EquipmentMarket panel_equipmentMarket;

    public void SetPanel(Panel_EquipmentMarket market)
    {
        panel_equipmentMarket = market;
        panel_equipmentMarket.OnPanelClosed += Close;
    }

    public void SetInstance(EquipmentInstance instance)
    {
        equipmentInstance = instance;
        UpdateAllInfo();
    }

    private void Close()
    {
        panel_equipmentMarket.ToggleConfrimOrderPopup(false);
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

        SetName(equipmentInstance.Name);
        SetIcon(equipmentInstance.Icon);
        SetPrice(equipmentInstance.CurrentPrice, equipmentInstance.IsRented);
        SetCondition(equipmentInstance.CurrentCondition);
    }

    public void SetName(string name)
    {
        Txt_name.text = name;
    }

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetPrice(int price, bool isRent)
    {
        if (!isRent)
        {
            Txt_price.text = $"${price.ToString("N0")}";
        }
        else
        {
            Txt_price.text = $"${MoneyController.Instance.CalculateEquipmentRent(price).ToString("N0")} / week";
        }
    }

    public void SetCondition(float condition)
    {
        Txt_condition.text = $"Condition: %{condition.ToString("F0")}";
    }

    public void SetOrderType(string type)
    {
        txt_OrderType.text = type;
    }

    public void SetDeiliveryPrice(int price)
    {
        txt_DeliveryPrice.text = $"Delivery Price: ${MarketController.Instance.GetDeliveryPrice(price)}";
    }

    public void UpdateConfirmBtn(bool value)
    {
        btn_Confirm.gameObject.SetActive(value);
        btn_Confirm.interactable = value;
    }
}
