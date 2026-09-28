using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentIDChecker : MonoBehaviour
{
    [SerializeField] string itemID;

    public bool isThisItem(string pItemID)
    {
        return itemID == pItemID;
    }
}
