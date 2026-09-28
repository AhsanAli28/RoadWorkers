using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject newGameConfirm;
    [SerializeField] SaveChecker saveChecker;
    [SerializeField] Button btn_Continue;
    [SerializeField] TextMeshProUGUI txt_version;
    [SerializeField] Panel_SettingsMenu panel_settingsMenu;

    private bool loadingGame = false;
    private void Start()
    {
        btn_Continue.interactable = saveChecker.DoesSaveFileExistPersistent();
        txt_version.text = Application.version;
        UpdateVolumeSettings();

        // Play BG music
        AudioManager.Instance?.StopBGMusic(0);
        AudioManager.Instance?.StopBGMusic(1);
        AudioManager.Instance?.Play_MainThemeMusic();
    }

    public void Btn_Continue()
    {
        if (loadingGame)
            return;

        PlayerPrefs.SetInt("LoadGame", 1); // 1 = Load Game
        SceneController.Instance.LoadMainScene();
        loadingGame = true;
    }

    public void Btn_NewGame()
    {
        if (loadingGame)
            return;

        if (saveChecker.DoesSaveFileExistPersistent())
        {
            ToggleNewGameConfirm(true);
        }
        else
        {

            SceneController.Instance.LoadMainScene();
            loadingGame = true;
        }
    }

    public void Btn_ConfirmNewGame()
    {
        if (loadingGame)
            return;

        ToggleNewGameConfirm(false);
        PlayerPrefs.SetInt("LoadGame", 0); // 0 = New Game

        SceneController.Instance.LoadMainScene();
        loadingGame = true;
    }

    public void Btn_Settings()
    {
        panel_settingsMenu.gameObject.SetActive(true);
    }

    public void Btn_CloseSettings()
    {
        panel_settingsMenu.gameObject.SetActive(false);
    }

    public void Btn_Quit()
    {
        Application.Quit();
    }

    public void ToggleNewGameConfirm(bool value)
    {
        newGameConfirm.SetActive(value);
    }

    public void UpdateVolumeSettings()
    {
       VolumeSettingsData data = AudioManager.Instance.GetVolumeSettingsFromPrefs();
        panel_settingsMenu.SetMasterSlider(data.MasterVolume);
        panel_settingsMenu.SetMusicSlider(data.MusicVolume);
        panel_settingsMenu.SetSfxSlider(data.SFXVolume);
    }

}
