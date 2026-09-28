using Spirit604.Gameplay.Config;
using Spirit604.MainMenu.Model;
using Spirit604.MainMenu.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.MainMenu.Controller
{
    public class MainMenuSceneSelectionController : MonoBehaviour
    {
        private const string PrefsKey = "PlayerMenuSettingsKey";

        [SerializeField] private MainMenuSceneSelectionView mainMenuSceneSelectionView;
        [SerializeField] private MainMenuConfigView mainMenuConfigView;
        [SerializeField] private MainMenuBottomView mainMenuBottomView;
        [SerializeField] private MainMenuToolbarView mainMenuToolbarView;

        [SerializeField] private bool forceMobileConfigs;
        [SerializeField] private List<LoadSceneDataConfig> loadSceneDataConfigs = new List<LoadSceneDataConfig>();
        [SerializeField] private List<LoadSceneDataConfig> loadSceneDataConfigsMobile = new List<LoadSceneDataConfig>();

        private PlayerMenuSettings playerMenuSettings = new PlayerMenuSettings();
        private ScriptableObject lastSelectedConfig;

        public event Action<LoadSceneDataConfig> OnSceneLoad = delegate { };

        public static bool StartFromMenu { get; set; }

        private List<LoadSceneDataConfig> LoadSceneDataConfigs
        {
            get
            {
                if (IsMobile)
                {
                    return loadSceneDataConfigsMobile;
                }
                else
                {
                    return loadSceneDataConfigs;
                }
            }
        }

        public bool IsMobile => Application.isMobilePlatform || forceMobileConfigs;

        private void Awake()
        {
            LoadPrefs();
            var options = LoadSceneDataConfigs.Where(a => !string.IsNullOrEmpty(a.SceneName)).Select(a => a.SceneName).ToList();
            mainMenuSceneSelectionView.Initialize(options, playerMenuSettings.SelectedSceneIndex);

            mainMenuSceneSelectionView.OnSelectedSceneChanged += MainMenuSceneSelectionView_OnSelectedSceneChanged;
            mainMenuBottomView.OnResetClicked += MainMenuBottomView_OnResetClicked;
            mainMenuBottomView.OnLoadSceneButtonClicked += MainMenuBottomView_OnLoadSceneButtonClicked;
            mainMenuToolbarView.OnToolbarButtonClicked += MainMenuToolbarView_OnToolbarButtonClicked;
            mainMenuConfigView.OnConfigChanged += MainMenuConfigView_OnConfigChanged;
            StartFromMenu = true;
        }

        private void Start()
        {
            LoadAll();
        }

        private void OnDestroy()
        {
            SavePrefs();
        }

        private ScriptableObject TryToGetConfig(int index, LoadSceneDataConfig.LoadSceneDataConfigDictionary configData)
        {
            if (configData == null)
            {
                return null;
            }

            var currentIndex = 0;

            foreach (var item in configData)
            {
                if (currentIndex == index)
                {
                    return configData.GetConfig(item.Key, IsMobile);
                }
                else
                {
                    currentIndex++;
                }
            }

            return null;
        }

        private void LoadPrefs()
        {
            var json = PlayerPrefs.GetString(PrefsKey);

            if (!string.IsNullOrEmpty(json))
            {
                playerMenuSettings = JsonUtility.FromJson<PlayerMenuSettings>(json);
            }

            mainMenuToolbarView.SelectedTabIndex = playerMenuSettings.SelectedToolbarIndex;
            mainMenuConfigView.Init(playerMenuSettings);
        }

        private void SavePrefs()
        {
            var json = JsonUtility.ToJson(playerMenuSettings);

            PlayerPrefs.SetString(PrefsKey, json);
        }

        private void ResetConfig()
        {
            LoadConfig(playerMenuSettings.SelectedSceneIndex, true);

            foreach (var item in mainMenuConfigView.InitialValues)
            {
                playerMenuSettings.RemoveKey(lastSelectedConfig.ToString(), item.Key);
            }

            mainMenuToolbarView.SwitchEditState(false);

            SavePrefs();
        }

        private void LoadConfig(int index, bool initial = false)
        {
            playerMenuSettings.SelectedSceneIndex = index;
            var selectedLoadSceneDataConfig = LoadSceneDataConfigs[index];

            int toolbarIndex = mainMenuToolbarView.SelectedTabIndex;

            lastSelectedConfig = TryToGetConfig(toolbarIndex, selectedLoadSceneDataConfig.ConfigData);

            if (lastSelectedConfig == null)
            {
                lastSelectedConfig = TryToGetConfig(0, selectedLoadSceneDataConfig.ConfigData);
                mainMenuToolbarView.SelectedTabIndex = 0;
                playerMenuSettings.SelectedToolbarIndex = 0;
            }

            SavePrefs();
            mainMenuConfigView.DrawConfig(lastSelectedConfig, initial);
            mainMenuToolbarView.InitNewSceneConfig(selectedLoadSceneDataConfig, playerMenuSettings, IsMobile);
            mainMenuBottomView.SwitchResetState(mainMenuConfigView.HasCustomParam);
        }

        private void LoadAll()
        {
            foreach (var loadSceneDataConfig in LoadSceneDataConfigs)
            {
                if (!loadSceneDataConfig)
                {
                    continue;
                }

                foreach (var configData in loadSceneDataConfig.ConfigData)
                {
                    if (!configData.Value)
                    {
                        continue;
                    }

                    mainMenuConfigView.DrawConfig(configData.Value, false);
                }
            }

            LoadConfig(playerMenuSettings.SelectedSceneIndex);
        }

        private void MainMenuConfigView_OnConfigChanged(ParamData paramData)
        {
            playerMenuSettings.SaveData(paramData);
            mainMenuBottomView.SwitchResetState(true);
            mainMenuToolbarView.SwitchEditState(true);
            SavePrefs();
        }

        private void MainMenuSceneSelectionView_OnSelectedSceneChanged(int index)
        {
            LoadConfig(index);
        }

        private void MainMenuBottomView_OnResetClicked()
        {
            ResetConfig();
        }

        private void MainMenuBottomView_OnLoadSceneButtonClicked()
        {
            var sceneData = LoadSceneDataConfigs[playerMenuSettings.SelectedSceneIndex];
            OnSceneLoad(sceneData);
        }

        private void MainMenuToolbarView_OnToolbarButtonClicked(int index)
        {
            playerMenuSettings.SelectedToolbarIndex = index;
            SavePrefs();
            var configData = LoadSceneDataConfigs[playerMenuSettings.SelectedSceneIndex].ConfigData;
            var config = TryToGetConfig(index, configData);
            mainMenuConfigView.DrawConfig(config);
            mainMenuBottomView.SwitchResetState(mainMenuConfigView.HasCustomParam);
        }
    }
}