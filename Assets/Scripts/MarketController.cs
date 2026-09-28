using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketController : MonoBehaviour
{
    [SerializeField] ItemsDatabase itemsDatabase;
    public Transform marketEntry;
    public Transform equipmentMarketEntry;
    public Transform companyEntry;
    public Transform companyPedestrianEntry;
    public Transform equipmentMarketPedestrianEntry;

    public MarketData Data { get; private set; } = new MarketData();

    //Singleton 
    private static MarketController _instance;

    public static MarketController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<MarketController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<MarketController>().Length > 1)
                {
                    Debug.LogWarning("More than one MarketController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("MarketController");
                    _instance = singletonObject.AddComponent<MarketController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of MarketController in the scene
        if (_instance == null)
        {
            _instance = this;
        }

        TimeController.Instance.OnWeekPassed += RegenerateUsedVehicles;
    }

    private void Start()
    {
        GenerateNewVehicles();
        GenerateUsedVehicles();
    }

    public void HandleLoad()
    {
        LoadMarketData(SaveController.Instance.GetData().Market);
    }

    public void BuyEquipment(EquipmentInstance equipmentInstance, int price)
    {
        MoneyController.Instance.SpendMoney(price, "Equipment");

        // Save the pending delivery
        Data.PendingEquipments.Add(new PendingEquipmentPurchase
        {
            EquipmentID = equipmentInstance.ItemID,
            Price = price,
            IsDelivery = true,
        });

        SaveMarketData();

        SendEquipmentDelivery(equipmentInstance);
    }


    public void BuyEquipmentWithPickup(EquipmentInstance equipmentInstance, int price, Worker worker)
    {
        MoneyController.Instance.SpendMoney(price, "Equipment");

        Data.PendingEquipments.Add(new PendingEquipmentPurchase
        {
            EquipmentID = equipmentInstance.ItemID,
            Price = price,
            IsDelivery = false,
            PickupWorkerID = worker.workerID,
        });

        SaveMarketData();

        SendEquipmentPickup(equipmentInstance, worker);
    }


    private void SendEquipmentDelivery(EquipmentInstance equipmentInstance)
    {
        Transport spawnedTransport = SpawnTransportAndAssign(equipmentInstance);
        ParkingSpot parkingSpot = spawnedTransport.GetAssignedParkingSpot();

        TransportController.Instance.SendTransport(

    marketEntry.position,
    parkingSpot.transform.position,
    transport: spawnedTransport,
    onDestinationReached: () => DeliverEquipment(equipmentInstance),
    returnAfterArrival: false,
    destroyAfterReturn: false
);
    }

    private void SendEquipmentPickup(EquipmentInstance equipmentInstance, Worker worker)
    {
        Transport transport = SpawnTransportAndAssign(equipmentInstance);
        transport.ToggleTrnasport(false); // Hide it until the worker comes to pic
        worker.AssignDelivery(transport, equipmentInstance);
    }

    public Transport SpawnTransportAndAssign(EquipmentInstance equipmentInstance)
    {
        GameObject spawnedTransportObj = TransportController.Instance.SpawnTransport(equipmentInstance.TransportType, marketEntry.position);
        Transport spawnedTransport = spawnedTransportObj.GetComponent<Transport>();
        equipmentInstance.Transport = spawnedTransport;

        // Assign the parking spot
        CompanyController.Instance.AssignVehicleToSpot(spawnedTransport);
        ParkingSpot parkingSpot = CompanyController.Instance.GetTransportParkSpot(spawnedTransport);
        equipmentInstance.ParkingId = spawnedTransport.GetAssignedParkingSpotID();

        return spawnedTransport;
    }

    public void BuyMaterials(Dictionary<string, int> items, int price)
    {
        MoneyController.Instance.SpendMoney(price, "Materials");

        var itemsCopy = new Dictionary<string, int>(items);

        Data.PendingMaterials.Add(new PendingMaterialPurchase
        {
            Materials = new SerializableDictionary<string, int>(itemsCopy),
            Price = price
        });

        SaveMarketData();

        TransportController.Instance.SendTransport(
            marketEntry.position,
            companyEntry.position,
            type: TransportType.Van,
            onDestinationReached: () => DeliverMaterials(itemsCopy),
            returnAfterArrival: true,
            destroyAfterReturn: true,
            onReturnReached: () => Debug.Log("Returned to depot")
        );
    }


    public void DeliverMaterials(Dictionary<string, int> items)
    {
        InventoryController.Instance.AddAnyItems(items);
        print($"Items delivered. Count: {items.Count}");

        // Remove matching pending material
        Data.PendingMaterials.RemoveAll(p => DictionaryExtensions.DictionaryEquals(p.Materials.ToDictionary(), items));
        SaveMarketData();
    }


    public void DeliverEquipment(EquipmentInstance instance)
    {
        print($"Delivered equipment: {instance.Name}");
        CompanyController.Instance.ParkTransport(instance.Transport);
        InventoryController.Instance.AddAnyInstance(instance);
        instance.Transport.ClearAllListeners();

        // Remove matching pending equipment
        Data.PendingEquipments.RemoveAll(p => p.EquipmentID == instance.ItemID);
        SaveMarketData();
    }


    public int GetDeliveryPrice(int equipmentPrice)
    {
        // 5%
        return (int)Mathf.Clamp(equipmentPrice * 0.05f, 100f, 2000f);
    }

    public int GetPriceForCondition(int basePrice, float currentCondition)
    {
        // Price ranges with 25% to 75% discount, depending on 75% or 25% of condition

        // Clamp condition to the 25–75% range
        currentCondition = Mathf.Clamp(currentCondition, 25f, 75f);

        // Convert condition to [0, 1] where 0 = 75% condition, 1 = 25% condition
        float t = (75f - currentCondition) / 50f;

        // Interpolate price multiplier between 0.75 (25% off) to 0.25 (75% off)
        float priceMultiplier = Mathf.Lerp(0.75f, 0.25f, t);

        return Mathf.RoundToInt(basePrice * priceMultiplier);
    }

    private void GenerateNewVehicles()
    {
        List<EquipmentInstance> newVehicles = new List<EquipmentInstance>();

        foreach (EquipmentItem item in itemsDatabase.equipmentItems)
        {
            EquipmentInstance itemInstance = (EquipmentInstance)itemsDatabase.CreateItemInstance(item.ItemID);
            newVehicles.Add(itemInstance);
        }

        UIManager.Instance.PopulateEquipmentMarketNewVehicles(newVehicles);
    }

    private void GenerateUsedVehicles()
    {
        List<EquipmentInstance> usedVehicles = new List<EquipmentInstance>();

        foreach (EquipmentItem item in itemsDatabase.equipmentItems)
        {
            EquipmentInstance itemInstance = (EquipmentInstance)itemsDatabase.CreateItemInstance(item.ItemID);

            // Random condition from 75% to 25%
            itemInstance.SetCondition(UnityEngine.Random.Range(75, 24));
            itemInstance.CurrentPrice = GetPriceForCondition(item.Price, itemInstance.CurrentCondition);
            usedVehicles.Add(itemInstance);
        }

        UIManager.Instance.PopulateEquipmentMarketUsedVehicles(usedVehicles);
    }

    private void RegenerateUsedVehicles(DateTime _)
    {
        GenerateUsedVehicles();
    }

    #region Saving

    private void SaveMarketData()
    {
        SaveController.Instance.UpdateData(gameData =>
        {
            gameData.Market = Data;
        });
    }

    public void LoadMarketData(MarketData savedData)
    {
        Data = savedData ?? new MarketData();

        // Restart any pending deliveries
        foreach (var equipment in Data.PendingEquipments)
        {
            EquipmentInstance instance = (EquipmentInstance)itemsDatabase.CreateItemInstance(equipment.EquipmentID);
            instance.CurrentPrice = equipment.Price;

            if (equipment.IsDelivery)
            {
                SendEquipmentDelivery(instance);
            }
            else
            {
                Worker pickupWorker = WorkersController.Instance.GetWorkerByID(equipment.PickupWorkerID);
                SendEquipmentPickup(instance, pickupWorker);
            }
        }

        foreach (var material in Data.PendingMaterials)
        {
            var itemsCopy = new Dictionary<string, int>(material.Materials.ToDictionary());
            TransportController.Instance.SendTransport(
                marketEntry.position,
                companyEntry.position,
                type: TransportType.Van,
                onDestinationReached: () => DeliverMaterials(itemsCopy),
                returnAfterArrival: true,
                destroyAfterReturn: true,
                onReturnReached: () => Debug.Log("Returned to depot")
            );
        }
    }


    #endregion
}

[Serializable]
public class MarketData
{
    public List<PendingEquipmentPurchase> PendingEquipments = new List<PendingEquipmentPurchase>();
    public List<PendingMaterialPurchase> PendingMaterials = new List<PendingMaterialPurchase>();
}

[Serializable]
public class PendingEquipmentPurchase
{
    public string EquipmentID;           // EquipmentItem.ItemID
    public int Price;
    public bool IsDelivery;             // false = pickup
    public string PickupWorkerID;
    public int ParkingSpotID;           // If already assigned
}

[Serializable]
public class PendingMaterialPurchase
{
    public SerializableDictionary<string, int> Materials = new SerializableDictionary<string, int>();
    public int Price;
}


// Helper
public static class DictionaryExtensions
{
    public static bool DictionaryEquals<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
    {
        if (first.Count != second.Count)
            return false;

        foreach (var kvp in first)
        {
            if (!second.TryGetValue(kvp.Key, out TValue secondValue))
                return false;

            if (!EqualityComparer<TValue>.Default.Equals(kvp.Value, secondValue))
                return false;
        }

        return true;
    }
}

