using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadsController : MonoBehaviour
{
    public List<Road> Roads;

    //Singleton 
    private static RoadsController _instance;

    public static RoadsController Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<RoadsController>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<RoadsController>().Length > 1)
                {
                    Debug.LogWarning("More than one RoadsController instance found in the scene.");
                }

                // If no instance is found, create a new one
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("RoadsController");
                    _instance = singletonObject.AddComponent<RoadsController>();
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of RoadsController in the scene
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            // If an instance already exists, destroy this GameObject
            Destroy(gameObject);
        }
    }

    public void HandleLoad()
    {
        List<RoadDamageData> roadDamages = SaveController.Instance.GetData().RoadDamages;
        List<RoadData> roads = SaveController.Instance.GetData().Roads;

        // Set the durability without notification
        foreach (RoadData roadData in roads)
        {
            GetRoad(roadData.RoadID).durability = roadData.Durability;
            GetRoad(roadData.RoadID).SetData(roadData);
        }

        // Set the road damages
        foreach (RoadDamageData roadDamage in roadDamages)
        {
            Road road = GetRoad(roadDamage.ParentRoadID);
            road.LoadDamage(roadDamage);
        }
    }

    public Road GetRoad(string roadID)
    {
        return Roads.FirstOrDefault(x => x.RoadID == roadID);
    }

    public RoadDamage GetRoadDamage(string roadDamageByID)
    {
        if (string.IsNullOrEmpty(roadDamageByID))
        {
            Debug.LogWarning("Provided RoadDamageID is null or empty.");
            return null;
        }

        foreach (Road road in Roads)
        {
            if (road.damages == null) continue;

            RoadDamage attempt = road.damages
                .FirstOrDefault(x => string.Equals(x.RoadDamageID, roadDamageByID, StringComparison.OrdinalIgnoreCase));

            if (attempt != null)
                return attempt;
        }

        Debug.LogWarning($"Road damage with ID '{roadDamageByID}' was not found.");
        return null;
    }

}
