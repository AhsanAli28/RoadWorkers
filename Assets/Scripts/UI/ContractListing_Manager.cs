using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ContractListing_Manager : ContractListing
{
    [SerializeField] TextMeshProUGUI workersCountTxt;
    [SerializeField] Button assignButton;

    int workersCount;

    public void Btn_Assign()
    {
        //Panel_Office panel_office = panel as Panel_Office;

        //if (panel_office == null)
        //{
        //    Debug.LogError("Casting failed, passed wrong Panel type.");
        //    panel_office = FindObjectOfType<Panel_Office>();
        //}

        //panel_office.AssignWorker(contract);
        //workersCount++;
        //SetWorkersCount(workersCount);

    }

    public void ToggleAssignButtonInteract(bool pValue)
    {
        assignButton.interactable = pValue;
    }

    //public void SetWorkersCount(int pValue)
    //{
    //    workersCountTxt.text = pValue.ToString();
    //}
}
