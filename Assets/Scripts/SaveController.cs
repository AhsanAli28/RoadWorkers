using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Unity.Cinemachine.CinemachineSplineRoll;

public class SaveController : MonoBehaviour
{
    private static SaveController _instance;
    public static SaveController Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing SaveController in the scene
                _instance = FindObjectOfType<SaveController>();
            }
            return _instance;
        }
    }

    private string savePath;
    private GameData data;

    /// <summary>
    /// Fired after a successful Load(); subscribers get the fresh GameData.
    /// </summary>
    public event Action<GameData> OnDataLoaded;

    private void Awake()
    {
        // Enforce singleton + persist
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        TimeController.Instance.OnDayPassed += AutoSave;

        // Immediately load on startup
        //Load();
    }

    private void Update()
    {
        //DEBUG
        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    Save();
        //}

        //if (Input.GetKeyDown(KeyCode.P))
        //{
        //    data = CreateFirstData();
        //    Save();
        //}
    }


    private void OnApplicationQuit()
    {
        Save();
    }


    private void AutoSave(DateTime date)
    {
        Save();
    }

    /// <summary>
    /// Saves current _data to disk as JSON.
    /// </summary>
    public void Save()
    {
        if (data == null)
            data = CreateFirstData(); // default if none exists

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(savePath, json);
            Debug.Log($"[SaveController] Saved data to {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveController] Save failed: {e}");
        }
    }

    /// <summary>
    /// Loads from disk. If file missing or invalid, creates new default GameData.
    /// Invokes OnDataLoaded afterwards.
    /// </summary>
    public void Load()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                data = JsonUtility.FromJson<GameData>(json);
                Debug.Log($"[SaveController] Loaded data from {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveController] Load failed, resetting data: {e}");
                data = CreateFirstData();
            }
        }
        else
        {
            Debug.Log($"[SaveController] No save file found, creating new data.");
            data = CreateFirstData();
        }

        OnDataLoaded?.Invoke(data);
    }

    public void ResetSave()
    {
        data = null;

        Save();
    }

    /// <summary>
    /// Allows runtime access to the loaded data.
    /// </summary>
    public GameData GetData()
    {
        if (data == null) Load();
        return data;
    }

    /// <summary>
    /// Updates in‑memory data.
    /// </summary>
    public void UpdateData(Action<GameData> updater)
    {
        if (data == null) Load();
        updater(data);
        //Save();
    }

    private GameData CreateFirstData()
    {
        GameData data = new GameData();

        data.Money = 5000;
        data.Date = "2000-01-01T00:00:00.0000000";
        data.Loans = new List<Loan>();
        data.Transactions = new List<Transaction>();
        data.InventoryData = new InventoryData();
        data.CompanyData = new CompanyData();
        data.Contracts = new List<Contract>();
        data.Workers = new List<WorkerData>();
        data.Roads = new List<RoadData>();
        data.RoadDamages = new List<RoadDamageData>();
        data.Market = new MarketData();
        data.VolumeSettings = new VolumeSettingsData();

        // Add roads. As they are one item that is always there
        if (RoadsController.Instance != null)
        {
            foreach (Road road in RoadsController.Instance.Roads)
            {
                RoadData roadData = new RoadData
                {
                    RoadID = road.RoadID,
                    Durability = 100,
                };
                road.SetData(roadData);

                data.Roads.Add(roadData);
            }
        }


        return data;
    }
}

[Serializable]
public class GameData
{
    public int Money;
    public string Date;
    public List<Loan> Loans = new List<Loan>();
    public List<Transaction> Transactions = new List<Transaction>();
    public InventoryData InventoryData = new InventoryData();
    public CompanyData CompanyData = new CompanyData();
    public List<Contract> Contracts = new List<Contract>();
    public List<WorkerData> Workers = new List<WorkerData>();
    public List<RoadData> Roads = new List<RoadData>();
    public List<RoadDamageData> RoadDamages = new List<RoadDamageData>();
    public MarketData Market = new MarketData();
    public VolumeSettingsData VolumeSettings = new VolumeSettingsData();

    public void SetDate(DateTime time)
    {
        Date = time.ToString("o"); // "o" is ISO 8601
    }

    public DateTime GetDate()
    {
        return DateTime.Parse(Date, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }    
}
