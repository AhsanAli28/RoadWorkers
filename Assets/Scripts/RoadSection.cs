using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSection : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Road parentRoad;
    public RoadDamage roadDamage;
    public GameObject roadPatch;
    public bool isVerticalSection;
    public bool isSideWalkDownSide;
    public bool damaged = false;
    public bool hasPatch => roadPatch != null;
    public string RoadSectionID;

    /// <summary>
    /// Called once when the car is passing by the road
    /// </summary>
    public void Section_DamageRoad(float pDamageFactor)
    {
        parentRoad.Road_DamageRoad(pDamageFactor);
    }

    public void SetDamage(RoadDamage pDamageObject)
    {
        damaged = true;
        roadDamage = pDamageObject;

        //Special adjustment for the new damage on top of the road patch
        if (roadPatch)
        {
            if (pDamageObject.damageName != "Cracks")
            {
                RemovePatch();
            }
            //Eleveate the crack on top of the road patch so its vsible damage without destroying the patch
            else
            {
                pDamageObject.transform.position += new Vector3(0, 0.1f, 0);
            }
        }

    }

    public void FixDamage_RoadSection()
    {
        damaged = false;

        if (roadDamage != null)
            Destroy(roadDamage.gameObject);
    }

    public void RemovePatch()
    {
        if(hasPatch)
            Destroy(roadPatch);
    }
}
