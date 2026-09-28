#if UNITY_EDITOR
using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public class ConfigAutoSyncer : IDisposable
    {
        private float SyncDelayDuration = 0.2f;

        private Component sourceConfig;
        private Component targetConfig;
        private SerializedObject so1;
        private SerializedObject so2;
        private SubScene subScene;
        private bool subSceneWasClosed;
        private bool changed;
        private bool findConfig;

        private bool waitForSync = false;
        private float syncTime;

        private bool closing;
        private bool closed;

        private Dictionary<SerializedProperty, SerializedProperty> fieldsPairs = new Dictionary<SerializedProperty, SerializedProperty>();

        private CityEditorSettings CityEditorSettings => CityEditorSettings.GetOrCreateSettings();

        public event Action<bool> Synced = delegate { };

        public ConfigAutoSyncer(MonoBehaviour sourceConfig, SubScene subScene = null)
        {
            this.sourceConfig = sourceConfig;
            this.so1 = new SerializedObject(sourceConfig);
            this.subScene = subScene;

            PrefabUtility.prefabInstanceUpdated += PrefabUtility_prefabInstanceUpdated;
            PrefabUtility.prefabInstanceReverted += PrefabUtility_prefabInstanceReverted;

            if (subScene != null)
            {
                if (subScene.IsLoaded)
                {
                    InitTargetConfig();
                }
                else
                {
                    subSceneWasClosed = true;
                }
            }
        }

        private void InitTargetConfig(bool force = false)
        {
            if (targetConfig && !force)
                return;

            if (!sourceConfig)
                return;

            closed = false;
            closing = false;

            var targetConfigs = ObjectUtils.FindObjectsOfType(sourceConfig.GetType());

            for (int i = 0; i < targetConfigs?.Length; i++)
            {
                Component targetConfigTemp = targetConfigs[i];

                if (targetConfigTemp != sourceConfig)
                {
                    targetConfig = targetConfigTemp;
                    break;
                }
            }

            if (targetConfig)
            {
                fieldsPairs.Clear();

                so2 = new SerializedObject(targetConfig);
                var fields1 = sourceConfig.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var fields2 = targetConfig.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                for (int i = 0; i < fields1.Length; i++)
                {
                    FieldInfo field = fields1[i];

                    var field1 = so1.FindProperty(field.Name);
                    var field2 = so2.FindProperty(field.Name);

                    if (field1 != null && field2 != null)
                    {
                        bool add = true;

                        if (field1.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (field1.objectReferenceValue is not ScriptableObject)
                            {
                                add = false;
                            }
                        }

                        if (add)
                            fieldsPairs.Add(field1, field2);
                    }
                }
            }
        }

        public void Sync(bool force = false)
        {
            bool shouldSync = CityEditorSettings.SyncConfigOnChange;

            if (!shouldSync && !force)
            {
                Synced(false);
                return;
            }

            if (subScene == null || (!subScene.IsLoaded && !CityEditorSettings.AutoOpenClosedScene && !force))
            {
                Synced(false);
                return;
            }

            if (!waitForSync)
            {
                waitForSync = true;
                syncTime = (float)EditorApplication.timeSinceStartup + SyncDelayDuration;

                EditorApplication.update += SyncInternal;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool userDispose)
        {
            PrefabUtility.prefabInstanceUpdated -= PrefabUtility_prefabInstanceUpdated;
            PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;

            if (closing || closed)
                return;

            sourceConfig = null;
            targetConfig = null;
            so1 = null;
            so2 = null;
            fieldsPairs = null;

            if (subScene != null)
            {
                if (userDispose && !Application.isPlaying)
                {
                    try
                    {
                        if (changed)
                        {
                            changed = false;

                            if (CityEditorSettings.AutoSaveChanges)
                                EditorSceneManager.SaveScene(subScene.EditingScene);
                        }

                        if (subSceneWasClosed && CityEditorSettings.AutoCloseScene)
                        {
                            bool close = true;

                            if (Selection.activeGameObject != null && Selection.activeGameObject.scene != null && Selection.activeGameObject.scene.isSubScene || Selection.activeGameObject == subScene.gameObject)
                                close = false;

                            if (close)
                            {
                                closed = true;
                                CloseSceneAsync();
                            }
                        }
                    }
                    catch { }
                }

                if (!closed)
                    subScene = null;

                closed = true;
            }
        }

        private void CloseSceneAsync()
        {
            closing = true;
            EditorApplication.delayCall += CloseSceneAsyncDelay;
        }

        private void CloseSceneAsyncDelay()
        {
            EditorApplication.delayCall -= CloseSceneAsyncDelay;

            if (subScene != null)
            {
                try
                {
                    if (subScene.EditingScene != null)
                        EditorSceneManager.CloseScene(subScene.EditingScene, true);
                }
                catch { }
                finally
                {
                    subScene = null;
                }
            }

            closing = false;
        }

        private void SyncInternal()
        {
            if (closed)
            {
                EditorApplication.update -= SyncInternal;
                return;
            }

            if (subScene != null && !subScene.IsLoaded && !findConfig)
            {
                findConfig = true;
                var scene = EditorSceneManager.OpenScene(subScene.EditableScenePath, OpenSceneMode.Additive);
                scene.isSubScene = true;
            }

            if (syncTime > EditorApplication.timeSinceStartup)
                return;

            if (findConfig)
            {
                findConfig = false;
                InitTargetConfig(true);
            }

            waitForSync = false;
            EditorApplication.update -= SyncInternal;

            if (so1 == null || so2 == null)
            {
                Synced(false);
                return;
            }

            if (!IsNullOrDestroyed(so1))
            {
                so1.Update();
            }
            else
            {
                Dispose(false);
                Synced(false);
                return;
            }

            bool currentChanged = false;

            foreach (var fieldData in fieldsPairs)
            {
                var field1 = fieldData.Key;
                var field2 = fieldData.Value;

                try
                {
                    if (field1.isDefaultOverride != field2.isDefaultOverride || field1.prefabOverride != field2.prefabOverride)
                    {
                        PrefabUtility.prefabInstanceReverted -= PrefabUtility_prefabInstanceReverted;
                        PrefabUtility.RevertPropertyOverride(field2, InteractionMode.AutomatedAction);
                        PrefabUtility.prefabInstanceReverted += PrefabUtility_prefabInstanceReverted;
                        changed = true;
                    }
                }
                catch { }

                if (field1.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (field2.objectReferenceValue != field1.objectReferenceValue && field2.objectReferenceValue is ScriptableObject)
                    {
                        field2.objectReferenceValue = field1.objectReferenceValue;
                        changed = true;
                        currentChanged = true;
                    }
                }
                else
                {
                    if (field2.boxedValue != field1.boxedValue)
                    {
                        field2.boxedValue = field1.boxedValue;
                        changed = true;
                        currentChanged = true;
                    }
                }
            }

            if (currentChanged)
            {
                if (!IsNullOrDestroyed(so2))
                {
                    so2.ApplyModifiedPropertiesWithoutUndo();
                    so2.Update();
                    Synced(true);
                }
                else
                {
                    Dispose(false);
                }
            }

            Synced(false);
        }

        private bool IsNullOrDestroyed(System.Object obj)
        {
            if (object.ReferenceEquals(obj, null)) return true;

            if (obj is SerializedObject) return (obj as SerializedObject) == null;

            return false;
        }

        private void PrefabUtility_prefabInstanceReverted(GameObject obj)
        {
            Sync();
        }

        private void PrefabUtility_prefabInstanceUpdated(GameObject instance)
        {
            Sync();
        }
    }
}
#endif
