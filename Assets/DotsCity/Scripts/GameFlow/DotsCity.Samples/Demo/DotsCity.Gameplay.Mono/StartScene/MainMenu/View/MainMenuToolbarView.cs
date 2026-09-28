using Spirit604.Extensions;
using Spirit604.Gameplay.Config;
using Spirit604.MainMenu.Model;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Spirit604.MainMenu.UI
{
    public class MainMenuToolbarView : MonoBehaviour
    {
        [SerializeField] private Transform toolbarPanel;
        [SerializeField] private ToolbarButton toolbarButtonPrefab;

        private List<ToolbarButton> toolbarButtons = new List<ToolbarButton>();

        public int SelectedTabIndex { get; set; }

        public event Action<int> OnToolbarButtonClicked = delegate { };

        private void Awake()
        {
            Clear();
        }

        public void InitNewSceneConfig(LoadSceneDataConfig loadSceneDataConfig, PlayerMenuSettings playerMenuSettings, bool isMobile)
        {
            InitToolbarButtons(loadSceneDataConfig, playerMenuSettings, isMobile);
            SwitchToolbarButtonState(SelectedTabIndex, true);
        }

        public void SwitchEditState(bool edited)
        {
            toolbarButtons[SelectedTabIndex].SwitchIndicatorState(edited);
        }

        private void Clear()
        {
            toolbarButtons.Clear();
            TransformExtensions.ClearChilds(toolbarPanel);
        }

        private void SwitchToolbarButtonState(int index, bool isActive)
        {
            if (index >= 0 && toolbarButtons.Count > index)
            {
                if (isActive)
                {
                    toolbarButtons[index].Select();
                }
                else
                {
                    toolbarButtons[index].Unselect();
                }
            }
        }

        private void InitToolbarButtons(LoadSceneDataConfig loadSceneDataConfig, PlayerMenuSettings playerMenuSettings, bool isMobile)
        {
            Clear();

            int index = 0;

            foreach (var config in loadSceneDataConfig.ConfigData)
            {
                var configName = config.Key;
                var button = Instantiate(toolbarButtonPrefab, toolbarPanel);
                var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

                buttonText.SetText(configName);

                var i = index;

                Action callBack = () =>
                {
                    ToolbarButton_OnToolbarButtonClicked(i);
                };

                button.Initialize(configName, callBack);
                index++;

                toolbarButtons.Add(button);

                var currentConfig = loadSceneDataConfig.ConfigData.GetConfig(configName, isMobile);
                var hasConfig = playerMenuSettings.HasConfig(currentConfig.ToString());

                button.SwitchIndicatorState(hasConfig);
            }
        }

        private void ToolbarButton_OnToolbarButtonClicked(int newIndex)
        {
            SwitchToolbarButtonState(SelectedTabIndex, false);
            SelectedTabIndex = newIndex;
            SwitchToolbarButtonState(SelectedTabIndex, true);
            OnToolbarButtonClicked(newIndex);
        }
    }
}