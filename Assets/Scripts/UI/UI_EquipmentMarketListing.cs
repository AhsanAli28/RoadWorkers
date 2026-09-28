using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_EquipmentMarketListing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI Txt_name;
    [SerializeField] TextMeshProUGUI Txt_price;
    [SerializeField] TextMeshProUGUI Txt_condition;
    [SerializeField] Button btn_buy;
    [SerializeField] Button btn_rent;

    private EquipmentInstance equipmentInstance;
    private Panel_EquipmentMarket panel_equipmentMarket;

    private int price;
    private float condition;

    private void Awake()
    {
        MoneyController.Instance.OnMoneyChange += HandleDependenciesChanged;
        CompanyController.Instance.OnParkingSpaceUpdated += HandleDependenciesChanged;
        HandleDependenciesChanged();
    }

    private void OnDestroy()
    {
        if (MoneyController.Instance != null)
            MoneyController.Instance.OnMoneyChange -= HandleDependenciesChanged;
        if (CompanyController.Instance != null)
            CompanyController.Instance.OnParkingSpaceUpdated -= HandleDependenciesChanged;
    }

    private void HandleDependenciesChanged(int oldMoney, int _ = 0) // money passed for OnMoneyChange, ignored
    {
        int currentMoney = MoneyController.Instance.Money;
        bool hasParking = HasParkingSpace();

        btn_buy.interactable = currentMoney >= equipmentInstance.CurrentPrice && hasParking;
        btn_rent.interactable = currentMoney >= MoneyController.Instance.CalculateEquipmentRent(equipmentInstance.CurrentPrice) && hasParking;
    }

    private void HandleDependenciesChanged() // money passed for OnMoneyChange, ignored
    {
        int currentMoney = MoneyController.Instance.Money;
        bool hasParking = HasParkingSpace();

        btn_buy.interactable = currentMoney >= equipmentInstance.CurrentPrice && hasParking;
        btn_rent.interactable = currentMoney >= MoneyController.Instance.CalculateEquipmentRent(equipmentInstance.CurrentPrice) && hasParking;
    }

    private bool HasParkingSpace()
    {
        bool isLarge = TransportController.Instance.GetTransportSize(equipmentInstance.TransportType);
        return isLarge ? CompanyController.Instance.GetFreeLargeSpotCount() > 0 : CompanyController.Instance.GetFreeSmallSpotCount() > 0;
    }

    public void SetPanel(Panel_EquipmentMarket market)
    {
        panel_equipmentMarket = market;
    }

    public void SetInstance(EquipmentInstance instance)
    {
        equipmentInstance = instance;
        UpdateAllInfo();
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
        SetPrice(equipmentInstance.CurrentPrice);
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

    public void SetPrice(int price)
    {
        Txt_price.text = $"$ {price.ToString("N0")}";
        this.price = price;
    }

    public void SetCondition(float condition)
    {
        Txt_condition.text = $"Condition: %{condition.ToString("F0")}";
        this.condition = condition;
    }

    public int GetPrice()
    {
        return price;
    }

    public float GetCondition()
    {
        return condition;
    }

    public void Btn_Buy()
    {
        panel_equipmentMarket.SelectOrder_Buy(this, equipmentInstance);
    }

    public void Btn_Rent()
    {
        panel_equipmentMarket.SelectOrder_Rent(this, equipmentInstance);
    }
}
