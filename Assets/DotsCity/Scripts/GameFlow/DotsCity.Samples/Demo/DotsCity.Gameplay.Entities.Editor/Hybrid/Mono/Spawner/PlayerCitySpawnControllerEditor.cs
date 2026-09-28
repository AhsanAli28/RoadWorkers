#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Npc.Factory.Player;
using Spirit604.DotsCity.Simulation.Binding;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Player;
using System;
using System.Linq;
using UnityEditor;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    [CustomEditor(typeof(PlayerCitySpawnController))]
    public class PlayerCitySpawnControllerEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/playerSpawn.html";

        private PlayerCitySpawnController playerCitySpawnController;
        private string[] options;
        private int[] ids;
        private string[] npcOptions;

        private bool IsAvailable =>
            playerCitySpawnController.PlayerSpawnDataConfig &&
            playerCitySpawnController.PlayerCarPool &&
            playerCitySpawnController.VehicleDataHolder;

        private GeneralSettingData GeneralSettings => playerCitySpawnController.GeneralSettings;

        private PlayerSpawnDataConfig Config => playerCitySpawnController.PlayerSpawnDataConfig;

        private void OnEnable()
        {
            playerCitySpawnController = target as PlayerCitySpawnController;
            playerCitySpawnController.OnInspectorEnabled();
            InitVehicleOptions();
            InitNPCOptions();

            EntityRefEditorBinder.OnBind += EntityRefEditorBinder_OnBind;
        }

        private void OnDisable()
        {
            EntityRefEditorBinder.OnBind -= EntityRefEditorBinder_OnBind;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!DrawProp("playerSpawnDataConfig", "Assign PlayerSpawnDataConfig"))
                return;

            if (!DrawProp("citySettingsInitializer", "Assign General settings"))
                return;

            if (!DrawProp("playerCarPool", "Assign PlayerCarPool", () => InitVehicleOptions()))
                return;

            if (!DrawProp("vehicleDataHolder", "Assign VehicleDataHolder", () => InitVehicleOptions()))
                return;

            if (!DrawProp("playerNpcFactory", "VehicleDataHolder", () => InitVehicleOptions()))
                return;

            if (!DrawProp("playerHybridMonoNpcFactory", "Assign PlayerHybridMonoNpcFactory", () => InitNPCOptions()))
                return;

            if (!playerCitySpawnController.GeneralSettings)
            {
                EditorGUILayout.HelpBox("General settings is null", MessageType.Error);
                return;
            }

            var trafficControlServiceSo = new SerializedObject(playerCitySpawnController.PlayerSpawnTrafficControlService);
            trafficControlServiceSo.Update();

            var generalSo = new SerializedObject(playerCitySpawnController.GeneralSettings);
            generalSo.Update();

            var playerAgentTypeProp = generalSo.FindProperty("playerAgentType");
            var playerAgentType = (PlayerAgentType)playerAgentTypeProp.enumValueFlag;

            var configSo = new SerializedObject(playerCitySpawnController.PlayerSpawnDataConfig);
            configSo.Update();

            Action applySo = () =>
            {
                generalSo.ApplyModifiedProperties();
                configSo.ApplyModifiedProperties();
                trafficControlServiceSo.ApplyModifiedProperties();
                serializedObject.ApplyModifiedProperties();
            };

            var currentSpawnPlayerTypeProp = configSo.FindProperty("currentSpawnPlayerType");

            var spawnPlayerType = (PlayerSpawnDataConfig.SpawnPlayerType)currentSpawnPlayerTypeProp.enumValueIndex;

            InspectorExtension.DrawGroupBox("Assignments", () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(DocLink, yOffset: 20);

                switch (playerAgentType)
                {
                    case PlayerAgentType.Player:
                        DrawPlayerAssigments(spawnPlayerType);
                        break;
                    case PlayerAgentType.FreeFlyCamera:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("freeFlyCameraFactory"));
                        break;
                    case PlayerAgentType.PlayerTrafficControl:
                        PlayerSpawnTrafficControlServiceEditor.DrawCache(trafficControlServiceSo);
                        break;
                }
            });

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                EditorGUILayout.PropertyField(playerAgentTypeProp);

                switch (playerAgentType)
                {
                    case PlayerAgentType.Player:

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(generalSo.FindProperty("playerControllerType"));

                        if (EditorGUI.EndChangeCheck())
                        {
                            generalSo.ApplyModifiedProperties();
                            InitNPCOptions();
                        }

                        if (GeneralSettings.BuiltInSolution)
                        {
                            DrawPlayerSettings(currentSpawnPlayerTypeProp, spawnPlayerType);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPoint"));
                        }
                        else
                        {
                            EditorTipExtension.TryToShowInspectorTip("NpcHybridMono",
                             "The player NPC is completely handled & spawned by a custom user Monobehaviour script.");
                        }

                        break;
                    case PlayerAgentType.FreeFlyCamera:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnPoint"));
                        break;
                    case PlayerAgentType.PlayerTrafficControl:
                        PlayerSpawnTrafficControlServiceEditor.DrawSettings(trafficControlServiceSo, playerCitySpawnController.VehicleDataCollection);
                        break;
                }
            });

            applySo.Invoke();
        }

        private void DrawPlayerAssigments(PlayerSpawnDataConfig.SpawnPlayerType spawnPlayerType)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerTargetHandler"));

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawnDataConfig"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                InitVehicleOptions();
            }

            switch (spawnPlayerType)
            {
                case PlayerSpawnDataConfig.SpawnPlayerType.Npc:
                    {
                        switch (GeneralSettings.CurrentPlayerControllerType)
                        {
                            case GeneralSettingData.PlayerControllerType.BuiltIn:
                                {
                                    switch (GeneralSettings.PlayerSimulationType)
                                    {
                                        case PlayerSimulationType.HybridDOTS:
                                            {
                                                EditorGUILayout.ObjectField("Player Npc Factory", serializedObject.FindProperty("playerNpcFactory").objectReferenceValue, typeof(PlayerNpcFactory), false);
                                                break;
                                            }
                                        case PlayerSimulationType.HybridMono:
                                            {
                                                EditorGUILayout.ObjectField("Player Mono Npc Factory", serializedObject.FindProperty("playerMonoNpcFactory").objectReferenceValue, typeof(PlayerMonoNpcFactory), false);
                                                break;
                                            }
                                    }

                                    break;
                                }
                            case GeneralSettingData.PlayerControllerType.BuiltInCustom:
                                {
                                    EditorGUILayout.ObjectField("Player Hybrid Mono Npc Factory", serializedObject.FindProperty("playerHybridMonoNpcFactory").objectReferenceValue, typeof(PlayerHybridMonoNpcFactory), false);
                                    break;
                                }
                        }

                        break;
                    }
                case PlayerSpawnDataConfig.SpawnPlayerType.Car:
                    {
                        EditorGUILayout.ObjectField("Vehicle Collection", playerCitySpawnController.VehicleDataHolder.VehicleDataCollection, typeof(VehicleDataCollection), false);
                        EditorGUILayout.ObjectField("Player Car Pool", playerCitySpawnController.PlayerCarPool, typeof(PlayerCarPool), true);
                        break;
                    }
            }
        }

        private void DrawPlayerSettings(SerializedProperty currentSpawnPlayerTypeProp, PlayerSpawnDataConfig.SpawnPlayerType spawnPlayerType)
        {
            EditorGUILayout.PropertyField(currentSpawnPlayerTypeProp);

            switch (spawnPlayerType)
            {
                case PlayerSpawnDataConfig.SpawnPlayerType.Npc:
                    {
                        EditorGUI.BeginChangeCheck();

                        GeneralSettings.WorldSimulationType = (WorldSimulationType)EditorGUILayout.EnumPopup("World Simulation Type", GeneralSettings.WorldSimulationType);

                        if (EditorGUI.EndChangeCheck())
                        {
                            InitNPCOptions();
                        }

                        ShowNpcOptions();

                        switch (GeneralSettings.PlayerSimulationType)
                        {
                            case PlayerSimulationType.HybridDOTS:
                                break;
                            case PlayerSimulationType.HybridMono:
                                break;
                        }

                        break;
                    }
                case PlayerSpawnDataConfig.SpawnPlayerType.Car:
                    {
                        GeneralSettings.WorldSimulationType = (WorldSimulationType)EditorGUILayout.EnumPopup("World Simulation Type", GeneralSettings.WorldSimulationType);

                        if (ids?.Length > 0)
                        {
                            var selectedIndex = Config.SelectedCarModel;

                            if (!ids.Contains(selectedIndex))
                            {
                                selectedIndex = ids[0];
                            }

                            var sourceLocalIndex = Array.IndexOf(ids, selectedIndex);
                            var newLocalIndex = EditorGUILayout.Popup("Car Model", sourceLocalIndex, options);

                            if (newLocalIndex != sourceLocalIndex)
                            {
                                var newModelIndex = ids[newLocalIndex];
                                Config.SelectedCarModel = newModelIndex;
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Vehicle IDs not found. Make sure you have added cars to the Player Car Pool & Vehicle Collection.", MessageType.Warning);
                        }

                        break;
                    }
            }
        }

        private void ShowNpcOptions()
        {
            if (npcOptions != null && npcOptions.Length > 0)
            {
                var selectedID = Config.SelectedNpcID;

                var prevLocalIndex = Array.IndexOf(npcOptions, selectedID);

                if (prevLocalIndex == -1)
                {
                    prevLocalIndex = 0;
                    selectedID = npcOptions[0];
                }

                var newLocal = EditorGUILayout.Popup("Selected Player", prevLocalIndex, npcOptions);

                if (prevLocalIndex != newLocal)
                {
                    var newId = npcOptions[newLocal];
                    Config.SelectedNpcID = newId;
                }
            }
            else
            {
                if (GeneralSettings.DOTSSimulation)
                {
                    EditorGUILayout.HelpBox("Player Npc Factory is empty", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Player HybridMono Npc Factory is empty", MessageType.Info);
                }
            }
        }

        private bool DrawProp(string propName, string errorMessage, Action changedCallback = null)
        {
            var prop = serializedObject.FindProperty(propName);

            if (prop.objectReferenceValue == null)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(prop);

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    changedCallback?.Invoke();
                }

                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                return false;
            }

            return true;
        }

        private void InitVehicleOptions()
        {
            if (!IsAvailable)
                return;

            options = playerCitySpawnController.GetVehicleNames();
            ids = playerCitySpawnController.GetVehicleIds();
        }

        private void InitNPCOptions()
        {
            npcOptions = playerCitySpawnController.GetNpcOptions();
        }

        private void EntityRefEditorBinder_OnBind(EntityWeakRef obj)
        {
            Repaint();
        }
    }
}
#endif