using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkerListing_Assign : WorkerListing
{
    [SerializeField] Toggle toggle;

    #region Buttons

    public void Tgl_AssignWorker(bool value)
    {
        Panel_ContractAssignment panel_ContractAssignment = panel as Panel_ContractAssignment;

        if (panel_ContractAssignment != null)
        {
            if (value)
            {
                panel_ContractAssignment.SelectWorker(this);
            }
            else
            {
                panel_ContractAssignment.DeselectWorker(this);
            }
        }
    }

    public void DeselectToggle()
    {
        toggle.SetIsOnWithoutNotify(false);
    }

    public void ToggleButtonInteract(bool value)
    {
        toggle.interactable = value;
    }

    public override void UpdateAssignmentAvaialability(bool value)
    {
        bool availability = worker.status == WorkerStatus.Available && value;
        ToggleButtonInteract(availability);
    }

    #endregion
}
