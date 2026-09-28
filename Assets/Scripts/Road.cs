using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Road : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] float damageFlat = 1f;
    [SerializeField] float damageRate = 1f;
    //[SerializeField] float cracksSpawnThreshold = 80f;
    //[SerializeField] float potholesSpawnThreshold = 60f;
    //[SerializeField] float pipeslayingSpawnThreshold = 40f;

    [Header("References")]
    [SerializeField] private string roadID;
    public string RoadID => roadID;
    public List<RoadSection> RoadSections = new List<RoadSection>(); // Rigght now this list contains only some of the sections since the road damage size was increased
    public List<RoadSection> AllVisualRoadSections = new List<RoadSection>(); // Used only for the road repair for now 
    public GameObject roadPatchPrefab;
    public GameObject crackPrefab;
    public GameObject potholePrefab;
    public GameObject pipeLayingPrefab;
    public GameObject roadReconstructPrefab;
    [SerializeField] Material freshMaterial;
    [SerializeField] GameObject roadblockStart;
    [SerializeField] GameObject roadblockEnd;

    [Header("Variables")]
    private RoadData roadData;
    public List<RoadDamage> damages = new List<RoadDamage>();
    int roadDamagesCount = 0;
    int cracksCount = 0;
    int potholesCount = 0;
    int pipeLayingCount = 0;
    int damagesForReconstruction;
    bool isRoadBroken = false;
    private float lastDamageCheckDurability = 100f; // Assuming road starts at 100 durability

    public float durability = 100f;

    private void Awake()
    {
        AssignRoadSections();

        //Calculate 80% of road sections for when to reconstruct the road
        damagesForReconstruction = Mathf.Max(Mathf.RoundToInt(RoadSections.Count * 0.8f), 5); //at least 4 for reconstruction
    }

    private float damageTimer = 0f;
    private float damageInterval = 5f; // Try damaging once per 3 real-time second

    private void Update()
    {
        // TODO: replace with traffic actively damaging the road, not the timer 
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;

            // Still keep the randomness
            if (Random.value <= 0.025f) // 10% chance every second
            {
                Road_DamageRoad(1);
            }
        }

        //DEBUG Spawn damage on input
        if (Input.GetKeyDown(KeyCode.B))
        {
            durability -= 10;
            Road_DamageRoad(1);
        }
    }


    private void AssignRoadSections()
    {
        foreach (RoadSection section in RoadSections)
        {
            section.parentRoad = this;
            section.RoadSectionID = roadID + "_section" + RoadSections.IndexOf(section);
        }
    }

    public void Road_DamageRoad(float pDamageFactor)
    {
        // Calculate the damage to the road. Flat number multiplied by the damage factor by the car (adjustable per cars) then multiplied by the road damage rate (adjustable per road).
        float damage = damageFlat * pDamageFactor * damageRate;
        durability -= damage;
        if (durability < 0)
            durability = 0;

        TrySpawningRoadDamage();
        SaveData();
    }

    public void TrySpawningRoadDamage()
    {
        // We don't want to spawn more damages on the road that already needs full reconstruction
        if (isRoadBroken) return;

        // Only proceed if at least 5 points of durability have been lost since last check
        if (lastDamageCheckDurability - durability < 5f)
            return;

        lastDamageCheckDurability = durability;

        // Spawn Road Cracks when durability is between 70 and 80 (max 3)
        if (durability < 80f && durability >= 50f && cracksCount < 8)
        {
            float chance = (durability >= 50f) ? (80f - durability) / 10f : 1f;
            if (Random.value < chance)
            {
                SpawnDamage(crackPrefab);
                cracksCount++;
            }
        }

        // Spawn Potholes when durability is between 60 and 20 (max 2)
        if (durability < 70f && durability >= 20f)
        {
            float chance = (durability >= 20f) ? (70f - durability) / 10f : 1f;
            if (Random.value < chance)
            {
                SpawnDamage(potholePrefab);
                potholesCount++;
            }
        }

        // Spawn Pipe Laying when durability is between 40 and 20 (max 1)
        if (durability < 70f && durability >= 20f)
        {
            float chance = (durability >= 20f) ? (70f - durability) / 10f : 1f;
            if (Random.value < chance)
            {
                SpawnDamage(pipeLayingPrefab);
                pipeLayingCount++;
            }
        }

        // Spawn Road Reconstruction when so many damages
        if (roadDamagesCount >= damagesForReconstruction)
        {
            CancelRoadDamages();
            SpawnDamage(roadReconstructPrefab);
            isRoadBroken = true;
        }
    }



    /// <summary>
    /// Spawns damage prefab at the random avaiable road section.
    /// </summary>
    /// <param name="pDamagePrefab"></param>
    private void SpawnDamage(GameObject pDamagePrefab)
    {
        RoadSection roadSection = GetFixedRoadSection();
        if (roadSection == null)
        {
            Debug.LogWarning("Trying to spawn damage but ran out of free space");
            return;
        }

        GameObject damage = Instantiate(pDamagePrefab, transform);
        RoadDamage roadDamage = damage.GetComponent<RoadDamage>();
        roadSection.SetDamage(roadDamage);
        damage.transform.position = GetRoadSectionPos(roadSection);
        damage.transform.Rotate(GetRoadSectionRotation(roadSection));
        roadDamagesCount++;
        roadDamage.SetParentRoad(this, roadSection);
        roadDamage.AssignRoadDamageID(roadID + "_damage" + roadDamagesCount);
        roadDamage.SaveData();
        ContractsController.Instance.AddAvaiableContract(roadDamage);
        damages.Add(roadDamage);
        //print($"Spawned damage: {pDamagePrefab.name}");
    }

    public void LoadDamage(RoadDamageData roadDamageData)
    {
        if (roadDamageData == null)
            Debug.LogError("Road Damage data is null");

        RoadSection roadSection = GetRoadSectionID(roadDamageData.RoadSectionID);
        GameObject damageObj = Instantiate(GetRoadDamagePrefab(roadDamageData.Type), transform);
        RoadDamage roadDamage = damageObj.GetComponent<RoadDamage>();
        roadSection.SetDamage(roadDamage);
        damageObj.transform.position = GetRoadSectionPos(roadSection);
        damageObj.transform.Rotate(GetRoadSectionRotation(roadSection));
        roadDamagesCount++;
        roadDamage.SetData(roadDamageData);
        roadDamage.SetParentRoad(this, roadSection);
        roadDamage.AssignRoadDamageID(roadDamageData.RoadDamageID);
        roadDamage.progress = roadDamageData.Progress;
        roadDamage.isConstructing = roadDamageData.IsConstructing;

        damages.Add(roadDamage);
        print($"Loaded damage: {damages.Count}");
    }

    public void FixRoadSection(RoadSection pRoadSection)
    {
        if (pRoadSection != null)
        {
            if (damages.Contains(pRoadSection.roadDamage))
                damages.Remove(pRoadSection.roadDamage);

            pRoadSection.FixDamage_RoadSection();

            //Special fix for shole road reconstruction
            if (pRoadSection.roadDamage.damageName == "Road Reconstruction")
            {
                RepairRoad();
            }
            else
            {
                //Spawn road patch
                GameObject patch = Instantiate(roadPatchPrefab, transform);
                patch.transform.position = GetRoadSectionPos(pRoadSection);
                patch.transform.Rotate(GetRoadSectionRotation(pRoadSection));
                pRoadSection.roadPatch = patch;
            }
        }
        else
            Debug.LogError("Road section is null");
    }

    public RoadSection GetFixedRoadSection()
    {
        var unusedItems = RoadSections.Where(item => !item.damaged).ToList();
        if (unusedItems.Count == 0)
            return null;

        int index = Random.Range(0, unusedItems.Count);
        return unusedItems[index];
    }

    private Vector3 GetRoadSectionPos(RoadSection roadSection)
    {
        return roadSection.meshRenderer.bounds.center + new Vector3(0, 0.1f, 0); //0.1f offset to prevent z-fighting
    }

    private Vector3 GetRoadSectionRotation(RoadSection roadSection)
    {
        Vector3 rotation = Vector3.zero;

        // Rotate 90 degress to make it verticle 
        if (roadSection.isVerticalSection)
            rotation += new Vector3(0, 90, 0);

        // Rotate 180 degrees to flip the side of the sidewalk
        if (!roadSection.isSideWalkDownSide)
            rotation += new Vector3(0, 180, 0);

        return rotation;
    }

    public RoadSection GetRoadSectionID(string roadSectionID)
    {
        return RoadSections.FirstOrDefault(x => x.RoadSectionID == roadSectionID);
    }

    public void SetupRoadReconstruction()
    {

    }

    private void CancelRoadDamages()
    {
        foreach (RoadDamage damage in damages)
        {
            if (damage != null)
                damage.GetAssignedContract().SetContractState(ContractState.Canceled);
        }
    }

    public void RepairRoad()
    {
        durability = 100;
        roadDamagesCount = 0;
        cracksCount = 0;
        potholesCount = 0;
        pipeLayingCount = 0;
        isRoadBroken = false;

        foreach (RoadSection roadSection in RoadSections)
        {
            roadSection.FixDamage_RoadSection();
            roadSection.RemovePatch();
        }

        foreach (RoadSection roadSection2 in AllVisualRoadSections)
        {
            roadSection2.meshRenderer.material = freshMaterial;
        }
    }

    public GameObject GetRoadDamagePrefab(RoadDamageType roadDamageType)
    {
        switch (roadDamageType)
        {
            case RoadDamageType.Cracks:
                return crackPrefab;
            case RoadDamageType.Pothole:
                return potholePrefab;
            case RoadDamageType.PipeLaying:
                return pipeLayingPrefab;
            case RoadDamageType.RoadReconstruction:
                return roadReconstructPrefab;
            default:
                return null;
        }
    }

    private bool ShouldRotateDamage(RoadDamageType roadDamageType)
    {
        switch (roadDamageType)
        {
            case RoadDamageType.Cracks:
                return true;
            case RoadDamageType.Pothole:
                return true;
            case RoadDamageType.PipeLaying:
                return false;
            case RoadDamageType.RoadReconstruction:
                return true;
            default:
                return true;
        }
    }

    #region Saving 

    public void SaveData()
    {
        if (roadData == null)
        {
            roadData = new RoadData
            {
                RoadID = this.roadID,
                Durability = this.durability,
            };

            // Add it to GameData
            SaveController.Instance.UpdateData(gameData =>
            {
                gameData.Roads.Add(roadData);
            });
        }
        else
        {
            // Just update the reference directly
            roadData.RoadID = this.roadID;
            roadData.Durability = this.durability;
        }
    }

    public void SetData(RoadData pRoadData)
    {
        roadData = pRoadData;
    }
    #endregion
}

[System.Serializable]
public class RoadData
{
    public string RoadID;
    public float Durability;
}
