using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] Panel uiPopup;

    public void OpenBuildingPanel()
    {
        print($"Clicked on a building: {gameObject.name}");
        UIManager.Instance.ShowPanel(uiPopup);
    }
}
