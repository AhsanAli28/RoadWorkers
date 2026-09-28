using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHelper : MonoBehaviour
{
    [SerializeField] Panel assignedPanel;
    [SerializeField] Popup assignedPopup;
    public void DisablePanel()
    {
        if(assignedPanel != null)
        {
            assignedPanel.DisableObject();
        }

        if (assignedPopup != null)
        {
            assignedPopup.DisableObject();
        }
    }
}
