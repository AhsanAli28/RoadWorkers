using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Panel_EquipmentMarket : PanelTabs
{
    [SerializeField] ItemsDatabase itemsDatabase;
    [SerializeField] GameObject equipmentUIListingPrefab;
    [SerializeField] Transform newVehicles_contentParent;
    [SerializeField] Transform usedVehicles_contentParent;

    [Header("ConfirmOrder")]
    [SerializeField] GameObject confirmOrderPopup;
    [SerializeField] GameObject workerSelectorPopup;
    [SerializeField] GameObject workerListingPrefab;
    [SerializeField] RectTransform workersContent;
    [SerializeField] UI_ConfirmEquipmentOrder confirmOrderInfo;

    private EquipmentInstance selectedOrder;
    private UI_EquipmentMarketListing selectedOrderListing;
    [SerializeField] private List<WorkerListing_Pickup> listedWorkers = new List<WorkerListing_Pickup>();
    private List<UI_EquipmentMarketListing> listedUsedVehicles = new List<UI_EquipmentMarketListing>();
    private WorkerListing_Pickup selectedWorker;

    bool isRentingOrder = false;

    private void Awake()
    {
        OnPanelClosed += DeselectAllWokrers;
    }

    private void OnDestroy()
    {
        OnPanelClosed -= DeselectAllWokrers;
    }

    protected override void Start()
    {
        base.Start();
    }

    public void SelectOrder_Buy(UI_EquipmentMarketListing uiListing, EquipmentInstance instance)
    {
        selectedOrder = instance;
        selectedOrderListing = uiListing;
        isRentingOrder = false;
        SetOrderInfo();
        ToggleConfrimOrderPopup(true);
    }

    public void SelectOrder_Rent(UI_EquipmentMarketListing uiListing, EquipmentInstance instance)
    {
        selectedOrder = instance;
        selectedOrderListing = uiListing;
        isRentingOrder = true;
        SetOrderInfo();
        ToggleConfrimOrderPopup(true);

    }

    public void Btn_DeliverOrder()
    {
        if (selectedOrder == null)
        {
            Debug.LogError("Order is null");
            return;
        }

        ToggleConfrimOrderPopup(false);

        // Get the right transport
        TransportType transportType = selectedOrder.TransportType;
        selectedOrder.IsRented = isRentingOrder;

        int deliveryPrice = MarketController.Instance.GetDeliveryPrice(selectedOrder.CurrentPrice);
        int purchasePrice = isRentingOrder ? MoneyController.Instance.CalculateEquipmentRent(selectedOrder.CurrentPrice) : selectedOrder.CurrentPrice;
        MoneyController.Instance.SpendMoney(deliveryPrice, "Delivery");
        MarketController.Instance.BuyEquipment(selectedOrder, purchasePrice);

        // Clear the listing instance. Copy the one we sent out and assign it again
        selectedOrderListing.SetInstance(selectedOrder.Clone());
    }

    public void Btn_PickupOrder()
    {
        ToggleWorkerSelector(true);
    }

    public void Btn_ConfirmPickup()
    {
        if (selectedOrder == null)
        {
            Debug.LogError("Order is null");
            return;
        }

        ToggleConfrimOrderPopup(false);

        // Get the right transport
        TransportType transportType = selectedOrder.TransportType;
        selectedOrder.IsRented = isRentingOrder;
        int purchasePrice = isRentingOrder ? MoneyController.Instance.CalculateEquipmentRent(selectedOrder.CurrentPrice) : selectedOrder.CurrentPrice;
        MarketController.Instance.BuyEquipmentWithPickup(selectedOrder, purchasePrice, selectedWorker.GetWorker());

        // Clear the listing instance. Copy the one we sent out and assign it again
        selectedOrderListing.SetInstance(selectedOrder.Clone());
    }

    public void PopulateNewEquipment(List<EquipmentInstance> newVehicles)
    {
        // Brand new
        foreach (EquipmentInstance instance in newVehicles)
        {
            GameObject equipmentListingObj = Instantiate(equipmentUIListingPrefab, newVehicles_contentParent);
            UI_EquipmentMarketListing equipmentListing = equipmentListingObj.GetComponent<UI_EquipmentMarketListing>();

            equipmentListing.SetPanel(this);
            equipmentListing.SetInstance(instance);
        }
    }

    public void PopulateUsedEquipment(List<EquipmentInstance> usedVehicles)
    {
        ClearUsedEquipment();

        // Used
        foreach (EquipmentInstance instance in usedVehicles)
        {
            GameObject equipmentListingObj = Instantiate(equipmentUIListingPrefab, usedVehicles_contentParent);
            UI_EquipmentMarketListing equipmentListing = equipmentListingObj.GetComponent<UI_EquipmentMarketListing>();

            equipmentListing.SetPanel(this);
            equipmentListing.SetInstance(instance);
            listedUsedVehicles.Add(equipmentListing);
        }
    }

    public void ClearUsedEquipment()
    {
        foreach (UI_EquipmentMarketListing item in listedUsedVehicles)
        {
            Destroy(item.gameObject);
        }

        listedUsedVehicles.Clear();
    }

    public void SetOrderInfo()
    {
        confirmOrderInfo.SetPanel(this);
        confirmOrderInfo.SetInstance(selectedOrder);
        confirmOrderInfo.SetName(selectedOrder.Name);
        confirmOrderInfo.SetIcon(selectedOrder.Icon);
        confirmOrderInfo.SetPrice(selectedOrder.CurrentPrice, isRentingOrder);
        confirmOrderInfo.SetCondition(selectedOrder.CurrentCondition);
        confirmOrderInfo.SetOrderType(isRentingOrder ? "Rent" : "Purchase");
        confirmOrderInfo.SetDeiliveryPrice(selectedOrder.CurrentPrice);
    }

    public void ToggleConfrimOrderPopup(bool value)
    {
        confirmOrderPopup.SetActive(value);
        ToggleWorkerSelector(false);
    }

    public void ToggleWorkerSelector(bool value)
    {
        if (workerSelectorPopup == null)
        {
            Debug.LogError("Null ref");
            return;
        }

        workerSelectorPopup.SetActive(value);
    }

    #region Workers listing

    public void AddWorkerToList(Worker pWorker)
    {
        GameObject listingObj = Instantiate(workerListingPrefab, workersContent);
        WorkerListing_Pickup workerListing = listingObj.GetComponent<WorkerListing_Pickup>();
        workerListing.SetWorker(this, pWorker);
        listedWorkers.Add(workerListing);
    }

    public void SelectWorker(WorkerListing_Pickup pWorkerUI)
    {
        DeselectAllWokrers();
        pWorkerUI.ToggleSelect(true);
        selectedWorker = pWorkerUI;
        confirmOrderInfo.UpdateConfirmBtn(selectedWorker != null);
    }

    public void DeselectWorker(WorkerListing_Pickup pWorkerUI)
    {
        if (pWorkerUI == selectedWorker)
            selectedWorker = null;

        pWorkerUI.DeselectToggle();
        pWorkerUI.ToggleSelect(false);
        confirmOrderInfo.UpdateConfirmBtn(selectedWorker != null);
    }

    public void DeselectAllWokrers()
    {
        for (int i = 0; i < listedWorkers.Count; i++)
        {
            DeselectWorker(listedWorkers[i]);
        }
    }

    #endregion
}

