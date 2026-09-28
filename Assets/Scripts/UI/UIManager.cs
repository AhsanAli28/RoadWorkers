using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] Panel_Office panel_Office;
    [SerializeField] Panel_CityHall panel_cityHall;
    [SerializeField] Panel_Bank panel_bank;
    [SerializeField] Panel_JobMarket panel_jobMarket;
    [SerializeField] Panel_ContractAssignment panel_ContractAssignment;
    [SerializeField] Panel_Gameplay panel_gameplayPanel;
    [SerializeField] Panel_MaterialsMarket panel_materialsMarket;
    [SerializeField] Panel_EquipmentMarket panel_equipmentMarket;
    [SerializeField] Panel_Options panel_options;
    [SerializeField] Panel_SettingsMenu panel_settingsMenu;

    [Header("References")]
    [SerializeField] ItemsDatabase inventoryDatabase;

    private Panel currentPanel;
    private Popup currentPopup;

    //Singleton 
    private static UIManager _instance;

    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // If there is no instance, find one in the scene
                _instance = FindObjectOfType<UIManager>();

                // If there is more than one instance, log a warning
                if (FindObjectsOfType<UIManager>().Length > 1)
                {
                    Debug.LogWarning("More than one UIManager instance found in the scene.");
                }
            }

            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure that there is only one instance of UIManager in the scene
        if (_instance == null)
        {
            _instance = this;
        }
    }

    public void ShowPanel(Panel panelToShow)
    {
        //Dont change of its the same screen
        if (panelToShow == currentPanel)
            return;

        if (currentPanel != null)
            currentPanel.ClosePanel();

        currentPanel = panelToShow;

        if (currentPanel != null)
        {
            currentPanel.EnableObject();
            ToggleGamePlayPanel(false);

            //Fade in animation
            if (currentPanel.animator != null && currentPanel.AnimateFade)
            {
                currentPanel.animator.Play("Panel_Fade_In");
                AudioManager.Instance?.Play_OpeningPopup();
            }
        }
        else
            Debug.LogError("Trying to show a null panel.");
    }

    public void CloseCurrentPanel()
    {
        if (currentPanel == null)
            return;

        currentPanel.ClosePanel();
        currentPanel = null;
        ToggleGamePlayPanel(true);
    }

    public void CloseCurrentPopup()
    {
        if (currentPopup == null) return;

        currentPopup.Pop_Out();
        currentPopup = null;
    }

    public void ToggleGamePlayPanel(bool value)
    {
        if (value)
            panel_gameplayPanel.EnableObject();
        else
            panel_gameplayPanel.DisableObject();
    }

    public void SetCurrentPopup(Popup pPopup)
    {
        currentPopup = pPopup;
    }

    public void SetContractAssignmentInfo(RoadDamage roadDamage, Contract contract)
    {
        panel_ContractAssignment.SetInfo(roadDamage, contract);
    }


    public void SetTimeAndDate(DateTime time)
    {
        panel_gameplayPanel.SetTimeAndDate(time);
    }

    public void ShowContractAssignment()
    {
        ShowPanel(panel_ContractAssignment);
    }

    public void SetLoans(List<Loan> loans)
    {
        panel_bank.SetupLoans(loans);
    }

    public void AddContractToList(Contract pContract)
    {
        panel_cityHall.AddContractToList(pContract);
    }

    public void AddWorkerToJobMarketList(Worker worker)
    {
        panel_jobMarket.AddWorkerHireToList(worker);
    }

    public void RemoveWorkerFromJobMarketList(Worker worker)
    {
        panel_jobMarket.RemoveWorkerHireFromList(worker);
    }

    public void AddWorkerToHiredLists(Worker pWorker)
    {
        panel_Office.AddWorkerToList(pWorker);
        panel_ContractAssignment.AddWorkerToList(pWorker);
        panel_equipmentMarket.AddWorkerToList(pWorker);
    }

    public void AddContractManagerToList(Contract pContract)
    {
        panel_Office.AddContractToList(pContract);
    }

    public void PopulateEquipmentMarketUsedVehicles(List<EquipmentInstance> vehiclesInstances)
    {
        panel_equipmentMarket.PopulateUsedEquipment(vehiclesInstances);
    }

    public void PopulateEquipmentMarketNewVehicles(List<EquipmentInstance> vehiclesInstances)
    {
        panel_equipmentMarket.PopulateNewEquipment(vehiclesInstances);
    }

    public void RefreshBankUI()
    {
        panel_bank.SetInfo();
    }

    public bool IsMouseOverPopup()
    {
        if (currentPopup == null) return false;

        Vector2 mousePos = Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)currentPopup.transform, mousePos, Camera.main);
    }

    public ItemData GetInventoryItem(string itemID)
    {
        return inventoryDatabase.GetItem(itemID);
    }

    public void UpdateParkingUpgradeButtons(int currentParking)
    {
        panel_Office.UpdateParkingUpgradeIcons(currentParking);
    }

    public void UpdateVolumeSettings(VolumeSettingsData volumeData)
    {
        panel_settingsMenu.SetMasterSlider(volumeData.MasterVolume);
        panel_settingsMenu.SetMusicSlider(volumeData.MusicVolume);
        panel_settingsMenu.SetSfxSlider(volumeData.SFXVolume);
    }
    
    public void Btn_Debug()
    {
        print("Button pressed!");
    }

}
