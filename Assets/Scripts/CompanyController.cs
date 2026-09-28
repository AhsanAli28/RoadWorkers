using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Manages company assets: parking spot setup, counts, and fleet vehicle assignments.
/// </summary>
public class CompanyController : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private List<ParkingSpot> allParkingSpots = new List<ParkingSpot>();
    [SerializeField] ParticleSystem upgradeSpecialEffect;

    [Header("Variables")]
    private List<ParkingSpot> builtParkingSpots = new List<ParkingSpot>();
    private List<ParkingSpot> smallParkingSpots = new List<ParkingSpot>();
    private List<ParkingSpot> largeParkingSpots = new List<ParkingSpot>();
    private int builtParkingSpotsCount => smallParkingSpots.Count + largeParkingSpots.Count;

    public Action OnParkingSpaceUpdated;

    // Singleton instance
    private static CompanyController _instance;
    public static CompanyController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CompanyController>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Singleton enforcement
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("Multiple CompanyController instances found, destroying duplicate.");
            return;
        }

    }


    private void Start()
    {
        UIManager.Instance.UpdateParkingUpgradeButtons(smallParkingSpots.Count + largeParkingSpots.Count); // LOAD
    }

    public void HandleLoad()
    {
        // Parking buildings 
        List<bool> parkings = new List<bool>(SaveController.Instance.GetData().CompanyData.BuiltParkings);
        foreach (bool isLarge in parkings)
        {
            if (isLarge)
            {
                BuildLargeSpot();
            }
            else
            {
                BuildSmallSpot();
            }
        }
    }

    #region Fleet

    public void BuySmallSpot()
    {
        ParkingSpot spot = BuildSmallSpot();
        if (spot != null)
        {
            int cost = 800;
            MoneyController.Instance.SpendMoney(cost, "Company upgrade");
            spot.PlaySpecialEffect();
            PlayUpgradeEffect();
            SaveData();
        }
    }

    /// <summary>
    /// Builds the next small parking spot.
    /// </summary>
    private ParkingSpot BuildSmallSpot()
    {
        if (builtParkingSpotsCount >= allParkingSpots.Count)
        {
            Debug.Log("Parking spot limit reached.");
            return null;
        }


        ParkingSpot spot = GetFirstLockedSpot();
        spot.isLarge = false;
        spot.ToggleParkingSpot(true);
        smallParkingSpots.Add(spot);
        builtParkingSpots.Add(spot);
        UIManager.Instance.UpdateParkingUpgradeButtons(builtParkingSpotsCount);
        OnParkingSpaceUpdated?.Invoke();

        return spot;
    }


    public void BuyLargeSpot()
    {
        ParkingSpot spot = BuildLargeSpot();

        if (spot != null)
        {
            int cost = 1200;
            MoneyController.Instance.SpendMoney(cost, "Company upgrade");
            spot.PlaySpecialEffect();
            PlayUpgradeEffect();
            SaveData();
        }

    }

    /// <summary>
    /// Builds the next large parking spot.
    /// </summary>
    private ParkingSpot BuildLargeSpot()
    {
        if (builtParkingSpotsCount >= allParkingSpots.Count)
        {
            Debug.Log("Parking spot limit reached.");
            return null;
        }

        ParkingSpot spot = GetFirstLockedSpot();
        spot.isLarge = true;
        spot.ToggleParkingSpot(true);
        largeParkingSpots.Add(spot);
        builtParkingSpots.Add(spot);
        UIManager.Instance.UpdateParkingUpgradeButtons(builtParkingSpotsCount);
        OnParkingSpaceUpdated?.Invoke();


        return spot;
    }

    public void ParkTransport(Transport transport)
    {
        // Get the parking spot first
        ParkingSpot parking = GetTransportParkSpot(transport);

        if (parking == null)
            return;

        Transform originalParent = transport.transform.parent;
        transport.transform.SetParent(parking.GetParkingPosition());
        transport.transform.localPosition = Vector3.zero;
        transport.transform.localRotation = Quaternion.identity;
        transport.transform.SetParent(originalParent);
    }

    /// <summary>
    /// Parents vehicle under the spot and resets local transform.
    /// </summary>
    public void AssignVehicleToSpot(Transport transport, ParkingSpot parkingSpot = null)
    {

        // Get first appropriate parking spot if unspecified
        if (parkingSpot == null)
        {
            if (!transport.isLarge)
            {
                ParkingSpot parking = GetFirstFreeSmallSpot();
                int spotID = allParkingSpots.IndexOf(parking);
                parking.assignedTransport = transport;
                transport.SetAssignedParkingSpot(spotID);
            }
            else if (transport.isLarge)
            {
                ParkingSpot parking = GetFirstFreeLargeSpot();
                int spotID = allParkingSpots.IndexOf(parking);
                parking.assignedTransport = transport;
                transport.SetAssignedParkingSpot(spotID);
            }
        }
        else
        {
            int spotID = allParkingSpots.IndexOf(parkingSpot);
            parkingSpot.assignedTransport = transport;
            transport.SetAssignedParkingSpot(spotID);
        }

        OnParkingSpaceUpdated?.Invoke();
    }

    /// <summary>
    /// Returns the count of free small spots.
    /// </summary>
    public int GetFreeSmallSpotCount()
    {
        int freeCount = 0;
        for (int i = 0; i < smallParkingSpots.Count; i++)
            if (smallParkingSpots[i].assignedTransport == null)
                freeCount++;
        return freeCount;
    }

    /// <summary>
    /// Returns the count of free large spots.
    /// </summary>
    public int GetFreeLargeSpotCount()
    {
        int freeCount = 0;
        for (int i = 0; i < largeParkingSpots.Count; i++)
            if (largeParkingSpots[i].assignedTransport == null)
                freeCount++;
        return freeCount;
    }


    public ParkingSpot GetParkingByID(int ID)
    {
        return allParkingSpots[ID];
    }

    private ParkingSpot GetFirstLockedSpot()
    {
        return allParkingSpots[builtParkingSpotsCount];
    }

    private ParkingSpot GetFirstFreeSmallSpot()
    {
        return smallParkingSpots.FirstOrDefault(s => s.assignedTransport == null);
    }

    private ParkingSpot GetFirstFreeLargeSpot()
    {
        return largeParkingSpots.FirstOrDefault(s => s.assignedTransport == null);
    }

    public ParkingSpot GetTransportParkSpot(Transport transport)
    {
        ParkingSpot parking = null;

        parking = allParkingSpots[transport.GetAssignedParkingSpotID()];

        if (parking == null)
        {
            Debug.LogError("Parking is null");
            return null;
        }

        return parking;
    }

    #endregion

    #region Saving

    public void SaveData()
    {
        CompanyData save = new CompanyData();

        // Parking spots
        foreach (var spot in builtParkingSpots)
        {
            save.BuiltParkings.Add(spot.isLarge);
        }

        SaveController.Instance.UpdateData(x => x.CompanyData = save);
    }

    public void LoadData()
    {

    }

    #endregion

    public void PlayUpgradeEffect()
    {
        if (upgradeSpecialEffect != null)
            upgradeSpecialEffect.Play();
        AudioManager.Instance?.Play_ShineMagic();
    }
}

[Serializable]
public class CompanyData
{
    public List<bool> BuiltParkings = new List<bool>(); // Bool = isLarge parking spot. If its not in the list, its not built
}