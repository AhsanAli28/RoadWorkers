using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    [Serializable]
    public class ProjectLayerData
    {
        public enum Status { Default, NewLayer, Exist, Collision, ExistWithOtherIndex }

        public int Group;
        public string Name;
        public int SourceLayer;
        public int CurrentLayer;
        public int ExistProjectLayer;
        public int LastChangeLayer;
        public bool SetLayerChilds;
        public List<GameObject> Prefabs = new List<GameObject>();
        public List<ChildObjectData> Childs = new List<ChildObjectData>();
        public List<PrefabData> DotsPhysicsPrefabs = new List<PrefabData>();
        public List<SceneAsset> Scenes = new List<SceneAsset>();
        public List<string> SceneObjectPatternNames = new List<string>();
        public List<string> SceneComponentPatternNames = new List<string>();
        public string RowText;
        public Status CurrentStatus;

        public int CurrentSourceLayer => LastChangeLayer == 0 ? SourceLayer : LastChangeLayer;

        public bool UpdateLayer(ref string[] strings)
        {
            if (CurrentLayer == CurrentSourceLayer && CurrentStatus == Status.Exist)
                return false;

            var currentLayer = CurrentLayer;

            if (CurrentStatus == Status.ExistWithOtherIndex)
            {
                currentLayer = ExistProjectLayer;
                CurrentLayer = ExistProjectLayer;
            }
            else
            {
                ProjectLayerManager.AddLayerAt(CurrentLayer, Name);
            }

            foreach (var prefab in Prefabs)
            {
                PrefabExtension.EditPrefab(prefab, (currentPrefab) =>
                {
                    if (!SetLayerChilds)
                    {
                        currentPrefab.layer = currentLayer;
                    }
                    else
                    {
                        IterateAllChilds(currentPrefab.transform, (child) =>
                        {
                            child.gameObject.layer = currentLayer;
                        }, true);
                    }
                });
            }

            foreach (var prefab in DotsPhysicsPrefabs)
            {
                PrefabExtension.EditPrefab(prefab.Component.gameObject, (currentPrefab) =>
                {
                    var prefabComponent = currentPrefab.GetComponent(prefab.Component.GetType());

                    var so = new SerializedObject(prefabComponent);
                    so.Update();

                    var sourcePropName = GetPropName(CurrentSourceLayer);
                    var newPropName = GetPropName(currentLayer);

                    var sourceProp = ProjectSettingsUtility.FindProperty(so, sourcePropName);
                    sourceProp.boolValue = false;

                    var newProp = ProjectSettingsUtility.FindProperty(so, newPropName);
                    newProp.boolValue = true;

                    so.ApplyModifiedProperties();
                });
            }

            foreach (var child in Childs)
            {
                PrefabExtension.EditPrefab(child.Prefab, (currentPrefab) =>
                {
                    foreach (var localChild in child.Childs)
                    {
                        var transform = currentPrefab.transform;

                        bool allChilds = false;

                        for (int i = 0; i < localChild.ChildIndexes.Length; i++)
                        {
                            int childIndex = localChild.ChildIndexes[i];

                            if (childIndex != -1)
                            {
                                transform = transform.GetChild(childIndex);
                            }
                            else
                            {
                                for (int j = 0; j < transform.childCount; j++)
                                {
                                    transform.GetChild(j).gameObject.layer = CurrentLayer;
                                }

                                allChilds = true;
                            }
                        }

                        if (!allChilds)
                        {
                            transform.gameObject.layer = CurrentLayer;
                        }
                    }
                });
            }

            bool changed = false;

            if (CurrentLayer != CurrentSourceLayer && !string.IsNullOrEmpty(RowText))
            {
                var row = GetRowByText(strings, RowText);

                if (row != -1)
                {
                    var newString = ProjectSettingsUtility.ReplaceConstantRow(strings[row], CurrentLayer);
                    strings[row] = newString;
                    changed = true;
                }
            }

            //foreach(var child in Scenes)
            //{
            //    child.
            //    EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            //    EditorSceneManager.OpenScene()
            //}

            LastChangeLayer = CurrentLayer;
            return changed;
        }

        private void IterateAllChilds(Transform root, Action<Transform> action, bool rootObject = false)
        {
            if (rootObject)
            {
                action(root);
            }

            foreach (Transform child in root)
            {
                action(child);
                IterateAllChilds(child, action);
            }
        }

        private int GetRowByText(string[] strings, string targetText)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                string text = strings[i];

                if (!string.IsNullOrEmpty(text) && text.Contains(targetText))
                    return i;
            }

            return -1;
        }

        private void EditorSceneManager_sceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            throw new NotImplementedException();
        }

        private string GetPropName(int layer)
        {
            string newPropName;
            if (layer < 10)
            {
                newPropName = $"Category0{layer}";
            }
            else
            {
                newPropName = $"Category{layer}";
            }

            return newPropName;
        }
    }
}
