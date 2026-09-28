#if UNITY_EDITOR
using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor
{
    public class CityEditorHubWindow : EditorWindowBase
    {
        internal const string SelectedHubIndexKey = "SelectedHubIndexKey";
        internal const string IgnoreHubWindowKey = "IgnoreHubWindowKey";

        internal static int SelectedHubIndex
        {
            get
            {
                return EditorPrefs.GetInt(SelectedHubIndexKey, 1);
            }
            set
            {
                EditorPrefs.SetInt(SelectedHubIndexKey, value);
            }
        }

        internal static bool SelectedHubIgnore
        {
            get
            {
                return EditorPrefs.GetBool(IgnoreHubWindowKey, false);
            }
            set
            {
                EditorPrefs.SetBool(IgnoreHubWindowKey, value);
            }
        }

        internal readonly static string[] Texts = new string[2]
        {
            "Hub", "Hub Base"
        };

        public static CityEditorHubWindow ShowWindow()
        {
            if (EditorPrefs.HasKey(IgnoreHubWindowKey) && SelectedHubIgnore)
            {
                Create(SelectedHubIndex);
                return null;
            }

            CityEditorHubWindow window = (CityEditorHubWindow)GetWindow(typeof(CityEditorHubWindow));
            window.titleContent = new GUIContent("Hub Selection Window");
            return window;
        }

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(350, 350);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Hub Variants:", EditorStyles.boldLabel);
            SelectedHubIndex = GUILayout.SelectionGrid(SelectedHubIndex, Texts, 1, GUILayout.Height(100f), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            string text = string.Empty;
            switch (SelectedHubIndex)
            {
                case 0:
                    {
                        text = "Root prefab used in Demo & Demo mono scenes & contains demo sample dependencies.";
                        break;
                    }
                case 1:
                    {
                        text = "Clean root prefab with just traffic & pedestrian simulation without any extra stuff.";
                        break;
                    }
            }

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(text, MessageType.Info, true);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            if (GUILayout.Button("Create", GUILayout.Height(50f), GUILayout.ExpandWidth(true)))
            {
                Create(SelectedHubIndex);
                Close();
            }

            SelectedHubIgnore = EditorGUILayout.Toggle("Never ask again", SelectedHubIgnore);

            EditorGUILayout.EndVertical();
        }

        public static void Create(int variant, bool allowSearchRoot = true)
        {
            var prefabs = AssetDatabaseExtension.TryGetUnityObjectsOfTypeFromPath<GameObject>(CityEditorBookmarks.CITY_BASE_PATH);

            if ((prefabs == null || prefabs.Count == 0) && allowSearchRoot)
            {
                CityEditorStartup.ForceUpdateRoot();
                Create(variant, false);
                return;
            }

            string ignoreText = "";

            switch (variant)
            {
                case 0:
                    {
                        ignoreText = "HubBase";
                        break;
                    }
                case 1:
                    {
                        ignoreText = "Hub";
                        break;
                    }
            }

            for (int i = 0; i < prefabs?.Count; i++)
            {
#if !DOTS_SIMULATION
                if (prefabs[i].name == ignoreText)
                    continue;
#else
                if (prefabs[i].name == "Hub")
                    continue;
#endif

                var createdObject = PrefabUtility.InstantiatePrefab(prefabs[i]) as GameObject;
                createdObject.transform.SetAsLastSibling();
            }

            if (variant == 1)
            {
                GenerateCullPoint();
            }
        }

        private static void GenerateCullPoint()
        {
            var type = Type.GetType("Spirit604.DotsCity.Hybrid.Core.Authoring.CullPointRuntimeAuthoring, 604Spirit.DotsCity.Hybrid.Core");

            if (type == null)
            {
                Debug.Log("GenerateCullPoint failed. CullPointRuntimeAuthoring type not found.");
                return;
            }

            var cullPoint = ObjectUtils.FindObjectOfType(type);

            if (cullPoint)
                return;

            var cullPointObj = new GameObject("CullPoint");
            cullPointObj.gameObject.AddComponent(type);

            var mainCamera = Camera.main;

            if (mainCamera != null)
            {
                SetObjectToCamera(cullPointObj, mainCamera);
                Debug.Log($"Cull point auto-generated as child to '{mainCamera.name}' main camera.");
            }
            else
            {
                var camera = ObjectUtils.FindObjectOfType<Camera>();

                if (camera != null)
                {
                    SetObjectToCamera(cullPointObj, camera);
                    Debug.Log($"Cull point auto-generated as child to '{camera.name}' camera. Make sure the cull point is the child of the main camera.");
                }
                else
                {
                    Debug.Log("Cull point auto-generated in the scene, but main camera with <b>MainCamera</b> tag not found.");
                }
            }
        }

        private static void SetObjectToCamera(GameObject cullPointObj, Camera mainCamera)
        {
            cullPointObj.transform.SetParent(mainCamera.transform);
            cullPointObj.transform.localPosition = Vector3.zero;
            cullPointObj.transform.localRotation = Quaternion.identity;
        }
    }
}
#endif
