using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkerListing_Pickup : WorkerListing
{
    [SerializeField] Toggle toggle;

    public void Tgl_AssignWorker(bool value)
    {
        Panel_EquipmentMarket panel_EquipmentMarket = panel as Panel_EquipmentMarket;

        if (panel_EquipmentMarket != null)
        {
            if (value)
            {
                panel_EquipmentMarket.SelectWorker(this);
            }
            else
            {
                panel_EquipmentMarket.DeselectWorker(this);
            }
        }

        toggle.SetIsOnWithoutNotify(value);
    }

    private void OnEnable()
    {
        UpdateAssignmentAvaialability(worker.status == WorkerStatus.Available);
    }

    public void DeselectToggle()
    {
        toggle.SetIsOnWithoutNotify(false);
    }

    public override void UpdateAssignmentAvaialability(bool value)
    {
        toggle.interactable = value;
    }
}
