using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Panel_SettingsMenu : Panel
{
    [SerializeField] Slider sl_masterVolume;
    [SerializeField] Slider sl_musicVolume;
    [SerializeField] Slider sl_sfxVolume;
    [SerializeField] Panel_Options panel_options;

    // Special close case for his little fella cuz I dont know, it breaks shit. TODO: Change it for whatever
    public void Btn_Close()
    {
        panelObj.SetActive(false);
        if (panel_options != null)
            UIManager.Instance.ShowPanel(panel_options);
    }

    public void SetMasterSlider(float value)
    {
        print("Setting slider");
        sl_masterVolume.SetValueWithoutNotify(value);
    }

    public void SetMusicSlider(float value)
    {
        sl_musicVolume.SetValueWithoutNotify(value);
    }

    public void SetSfxSlider(float value)
    {
        sl_sfxVolume.SetValueWithoutNotify(value);
    }

    public void OnMasterVolumeChange(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChange(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChange(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }
}
