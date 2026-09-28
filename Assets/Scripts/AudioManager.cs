using System;
using UnityEngine;
using UnityEngine.Audio;
using static Spirit604.DotsCity.RuntimeRoad.RuntimeSegment;
using static Unity.VisualScripting.Member;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("References")]
    public AudioMixer mainMixer;
    public AudioSource bgmSource1;
    public AudioSource bgmSource2;
    public AudioSource sfxSource;
    public AudioSource loopSource;

    // Custom saving via Player Prefs for the volume, because integrating it with the Save system in the Main Menu is pain in the ass
    [Header("Prefs saving")]
    const string MASTER_VOLUME = "MasterVolume";
    const string MUSIC_VOLUME = "MusicVolume";
    const string SFX_VOLUME = "SFXVolume";

    [Header("Audio assets")]
    // Background music
    [SerializeField] AudioClip wonderousWaters;
    [SerializeField] AudioClip mainThemeMusic;
    [SerializeField] AudioClip ingameSounds;
    // SFX
    [SerializeField] AudioClip buttonPress;
    [SerializeField] AudioClip cashierCoins;
    [SerializeField] AudioClip construction;
    [SerializeField] AudioClip drill;
    [SerializeField] AudioClip trucks;
    [SerializeField] AudioClip menuSelection;
    [SerializeField] AudioClip upgradePopup;
    [SerializeField] AudioClip openingPopup;
    [SerializeField] AudioClip shineMagic;

    private AudioClip currentMusic;
    private AudioClip currentSFXLoop;
    private VolumeSettingsData data = new VolumeSettingsData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadVolumeSettingsFromPrefs();
    }

    public void HandleLoad()
    {
        LoadVolumeSettingsFromPrefs();
    }

    public void PlayBGMusic(AudioClip clip, int sourceID = 0)
    {
        //Dont interrupt the flow if the music is already playing 
        if (currentMusic == clip)
            return;
        print("Playing bg music");
        currentMusic = clip;

        AudioSource audioSource = GetBackgroundSource(sourceID);
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopBGMusic(int sourceID = 0)
    {
        AudioSource audioSource = GetBackgroundSource(sourceID);
        audioSource.Stop();
        currentMusic = null;
        audioSource.clip = null;
        audioSource.loop = false;
    }

    public void PauseBGMusic(int sourceID = 0)
    {
        AudioSource audioSource = GetBackgroundSource(sourceID);
        audioSource.Pause();
    }

    public void ResumeBGMusic(int sourceID = 0)
    {
        AudioSource audioSource = GetBackgroundSource(sourceID);
        audioSource.UnPause();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Needs to be stopped manually.
    /// </summary>
    public void PlaySFXLoop(AudioClip clip)
    {
        if (currentSFXLoop == clip)
            return;

        currentSFXLoop = clip;
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.Play();
    }

    public void MuteAll(bool isMuted)
    {
        AudioListener.pause = isMuted;
    }

    public void StopLoopSound()
    {
        loopSource.Stop();
        currentSFXLoop = null;
        loopSource.clip = null;
        loopSource.loop = false;
    }

    private AudioSource GetBackgroundSource(int sourceID = 0)
    {
        return sourceID == 0 ? bgmSource1 : bgmSource2;
    }

    #region Volume settings

    public void SetMasterVolume(float value)
    {
        mainMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        data.MasterVolume = value;
        SaveController.Instance?.UpdateData(x => x.VolumeSettings = data);
        PlayerPrefs.SetFloat(MASTER_VOLUME, value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        data.MusicVolume = value;
        SaveController.Instance?.UpdateData(x => x.VolumeSettings = data);
        PlayerPrefs.SetFloat(MUSIC_VOLUME, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        data.SFXVolume = value;
        SaveController.Instance?.UpdateData(x => x.VolumeSettings = data);
        PlayerPrefs.SetFloat(SFX_VOLUME, value);
        PlayerPrefs.Save();
    }

    #endregion

    #region Playing specific sounds 

    public void Play_CitySounds()
    {
        PlayBGMusic(ingameSounds, 1);
    }

    public void Play_MainThemeMusic()
    {
        PlayBGMusic(mainThemeMusic);
    }

    public void Play_IngameMusic()
    {
        PlayBGMusic(wonderousWaters);
    }

    public void Play_ButtonPress()
    {
        PlaySFX(buttonPress);
    }

    public void Play_CashierCoins()
    {
        PlaySFX(cashierCoins);
    }

    public void Play_Construction()
    {
        PlaySFX(construction);
    }

    public void Play_Drill()
    {
        PlaySFX(drill);
    }

    public void Play_Trucks()
    {
        PlaySFX(trucks);
    }

    public void Play_MenuSelection()
    {
        PlaySFX(menuSelection);
    }

    public void Play_UpgradePopup()
    {
        PlaySFX(upgradePopup);
    }

    public void Play_OpeningPopup()
    {
        PlaySFX(openingPopup);
    }

    public void Play_ShineMagic()
    {
        PlaySFX(shineMagic);
    }

    #endregion


    #region Loading

    private void LoadVolumeSettings(VolumeSettingsData savedData)
    {
        this.data = savedData ?? new VolumeSettingsData();

        SetMasterVolume(data.MasterVolume);
        SetMusicVolume(data.MusicVolume);
        SetSFXVolume(data.SFXVolume);
        print("Loading volume");
        UIManager.Instance?.UpdateVolumeSettings(savedData);
    }

    private void LoadVolumeSettingsFromPrefs()
    {
        VolumeSettingsData data = GetVolumeSettingsFromPrefs();

        SetMasterVolume(data.MasterVolume);
        SetMusicVolume(data.MusicVolume);
        SetSFXVolume(data.SFXVolume);

        UIManager.Instance?.UpdateVolumeSettings(data);
    }

    public VolumeSettingsData GetVolumeSettingsFromPrefs()
    {
        float masterValue = PlayerPrefs.GetFloat(MASTER_VOLUME);
        float musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME);
        float sfxValue = PlayerPrefs.GetFloat(SFX_VOLUME);

        VolumeSettingsData data = new VolumeSettingsData();
        data.MasterVolume = masterValue;
        data.MusicVolume = musicVolume;
        data.SFXVolume = sfxValue;

        return data;
    }

    #endregion
}

[Serializable]
public class VolumeSettingsData
{
    public float MasterVolume = 1f;
    public float MusicVolume = 0.3f;
    public float SFXVolume = 1f;
}
