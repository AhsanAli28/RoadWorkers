using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class PrefabPreviewSaverWindow : EditorWindowBase
    {
        private const int LayerMask = 1 << 4;
        private const string LayerName = "Water";
        private const string DefaultPath = "Prefabs/RuntimeTileDemo/Data/Tile Grid View Icon Data.asset";
        private const string PreviewSavePath = "PreviewSavePathKey";

        private readonly int[] TextureSizes = new[] { 256, 512, 1024, 2048, 4096 };
        private const float OpenButtonWidth = 40f;
        private const float PlusButtonSize = 20f;
        private const float PreviewTextureHeight = 200;

        private readonly Vector3 CameraPositionDefault = new Vector3(25, 23, -19);
        private readonly Vector3 OriginPointDefault = new Vector3(-1.870302f, 0, 1.99338f);
        private readonly float FieldOfViewDefault = 30f;

        [SerializeField] private int indexSize = 1;
        [SerializeField] private TileGridViewIconData tileGridViewIconData;
        [SerializeField] private Vector3 cameraPosition = new Vector3(25, 23, -19);
        [SerializeField] private Vector3 originPoint = new Vector3(-1.870302f, 0, 1.99338f);
        [SerializeField][Range(0, 3)] private int objectRotation;
        [SerializeField] private float fieldOfView = 30f;
        [SerializeField] private string textureSavePath = "Assets/";

        [SerializeField]
        private List<GameObject> list = new List<GameObject>();

        private GameObject cameraObject;
        private Camera camera;
        private RenderTexture renderTexture;
        private Texture2D previewTexture;
        private int selectedIndex;
        private Vector2 scrollPos;

        private int TextureSize => TextureSizes[indexSize];

        private GameObject SelectedPrefab
        {
            get
            {
                if (list.Count > selectedIndex)
                {
                    return list[selectedIndex];
                }

                if (list.Count > 0)
                {
                    selectedIndex = 0;
                    return list[selectedIndex];
                }

                return null;
            }
        }

        private Color PreviewBoxColor
        {
            get
            {
                Color32 color = Color.gray;
                color.a = 50;
                return color;
            }
        }

        private string Key => $"{EditorExtension.GetUniquePrefsKey(PreviewSavePath)}";

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(380, 520);
        }

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Prefab Preview Saver")]
        public static PrefabPreviewSaverWindow ShowWindow()
        {
            var window = (PrefabPreviewSaverWindow)GetWindow(typeof(PrefabPreviewSaverWindow));
            window.titleContent = new GUIContent("Prefab Preview Saver");
            window.maxSize = window.GetDefaultWindowSize();
            return window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            cameraObject = new GameObject("PreviewCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;

            camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = Color.clear;
            camera.clearFlags = CameraClearFlags.Color;
            camera.cameraType = CameraType.Preview;
            camera.cullingMask = LayerMask;

            LoadData();
            UpdateCameraPosition();
            UpdateCameraFov();
            Initialize();

            if (list.Count > 0 && ((list[0] as GameObject) == null || !PrefabUtility.IsOutermostPrefabInstanceRoot(list[0] as GameObject)))
            {
                list.Clear();
            }

            InitRenderTexture();

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (cameraObject != null)
            {
                DestroyImmediate(cameraObject);
            }

            DestroyTexture();
            SaveData();

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
        }

        private void OnGUI()
        {
            var serializedObject = new SerializedObject(this);
            serializedObject.Update();

            if (list.Count > 0 && previewTexture == null)
            {
                UpdateTexture();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUI.BeginChangeCheck();

            indexSize = EditorGUILayout.IntSlider($"Texture Size [{TextureSizes[indexSize]}]", indexSize, 0, TextureSizes.Length);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                InitRenderTexture();
                UpdateTexture();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tileGridViewIconData"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SaveCustomIconData();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraPosition"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("originPoint"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateCameraPosition(true);
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("fieldOfView"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateCameraFov(true);
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("objectRotation"), new GUIContent($"Object Rotation {objectRotation * 90}°"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateTexture();
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(textureSavePath)));

            if (GUILayout.Button("Open", GUILayout.Width(OpenButtonWidth)))
            {
                AssetDatabaseExtension.SelectProjectFolder(textureSavePath);
            }

            if (GUILayout.Button("+", GUILayout.Width(PlusButtonSize)))
            {
                AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select texture save path", ref textureSavePath, textureSavePath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            var text = string.Empty;

            if (SelectedPrefab != null)
            {
                text = SelectedPrefab.name;
            }

            GUI.enabled = list.Count > 0;

            var width = EditorGUIUtility.labelWidth;

            selectedIndex = EditorGUILayout.IntSlider($"Prefab [{text}]", selectedIndex, 0, list.Count);

            EditorGUIUtility.labelWidth = 300f;

            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateTexture();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("list"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdateTexture();
            }


            var boxRect = GUILayoutUtility.GetLastRect();

            boxRect.y += boxRect.height;
            boxRect.height = PreviewTextureHeight;
            boxRect.x = boxRect.width / 4;
            boxRect.width /= 2;

            EditorGUILayout.GetControlRect(false, PreviewTextureHeight);
            EditorGUI.LabelField(boxRect, new GUIContent(previewTexture));

            EditorGUI.DrawRect(boxRect, PreviewBoxColor);

            if (GUILayout.Button("Reset View"))
            {
                ResetView();
            }

            if (GUILayout.Button("Create"))
            {
                Save();
            }

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void Save()
        {
            for (int i = 0; i < list.Count; i++)
            {
                var prefab = list[i];
                SaveAssetPreviewWithResolution(prefab);
            }

            AssetDatabase.Refresh();
        }

        private void UpdateTexture()
        {
            DestroyPreviewTexture();

            if (!SelectedPrefab) return;

            var tempObject = CreateTempObject(SelectedPrefab);
            var hPlane = new Plane(Vector3.up, Vector3.zero);

            //var ray = new Ray(camera.transform.position, camera.transform.forward);

            //if (hPlane.Raycast(ray, out var distance))
            //{
            //    new GameObject("").transform.position = ray.GetPoint(distance);
            //}

            camera.Render();

            RenderTexture.active = renderTexture;

            previewTexture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            previewTexture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
            previewTexture.Apply();

            RenderTexture.active = null;

            DestroyImmediate(tempObject);
            Repaint();
        }

        public void SaveAssetPreviewWithResolution(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);

            var tempObject = CreateTempObject(prefab);

            Texture2D texture = null;

            try
            {
                camera.Render();

                RenderTexture.active = renderTexture;

                texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, TextureSize, TextureSize), 0, 0);
                texture.Apply();

                // Encode texture to PNG
                byte[] pngData = texture.EncodeToPNG();
                if (pngData == null)
                {
                    Debug.LogError("Failed to encode texture to PNG!");
                }

                // Save the PNG file
                string directory = textureSavePath;
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                string textureAssetName = assetName + "_Preview";
                string fileName = $"{textureAssetName}.png";
                string fullPath = Path.Combine(directory, fileName);

                File.WriteAllBytes(fullPath, pngData);
                Debug.Log($"Asset preview saved to {fullPath}");

                AssetDatabase.Refresh();

                var textureImporter = AssetImporter.GetAtPath(fullPath) as TextureImporter;
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.SaveAndReimport();

                AssetDatabase.Refresh();

                var textureAsset = AssetDatabase.LoadAssetAtPath<Sprite>($"{fullPath}");
                var data = tileGridViewIconData.Data;

                if (!data.ContainsKey(assetName))
                {
                    data.Add(assetName, textureAsset);
                }
                else
                {
                    data[assetName] = textureAsset;
                }

                EditorSaver.SetObjectDirty(tileGridViewIconData);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                RenderTexture.active = null;

                if (texture != null)
                    Object.DestroyImmediate(texture);

                Object.DestroyImmediate(tempObject);
            }
        }

        private GameObject CreateTempObject(GameObject prefab)
        {
            var tempObject = Instantiate(prefab) as GameObject;

            tempObject.transform.Rotate(Vector3.up, objectRotation * 90);
            tempObject.gameObject.layer = UnityEngine.LayerMask.NameToLayer(LayerName);

            foreach (Transform t in tempObject.transform)
            {
                t.gameObject.layer = UnityEngine.LayerMask.NameToLayer(LayerName);
            }

            return tempObject;
        }

        private void DestroyTexture()
        {
            if (renderTexture != null)
            {
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            DestroyPreviewTexture();
        }

        private void DestroyPreviewTexture()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        private void InitRenderTexture()
        {
            DestroyTexture();
            renderTexture = new RenderTexture(TextureSize, TextureSize, 24);
            camera.targetTexture = renderTexture;
        }

        private void UpdateCameraPosition(bool user = false)
        {
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(originPoint, Vector3.up);

            if (user)
            {
                UpdateTexture();
            }
        }

        private void UpdateCameraFov(bool user = false)
        {
            camera.fieldOfView = fieldOfView;

            if (user)
            {
                UpdateTexture();
            }
        }

        private void Initialize()
        {
            if (tileGridViewIconData == null || ((tileGridViewIconData as ScriptableObject) == null))
            {
                var loaded = false;
                var savedPath = EditorPrefs.GetString(Key, string.Empty);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    tileGridViewIconData = AssetDatabase.LoadAssetAtPath<TileGridViewIconData>(savedPath);
                    loaded = tileGridViewIconData != null;
                }

                if (!loaded)
                {
                    var path = CityEditorBookmarks.GetPath(DefaultPath);
                    tileGridViewIconData = AssetDatabase.LoadAssetAtPath<TileGridViewIconData>(path);
                }
            }
        }

        private void SaveCustomIconData()
        {
            if (!tileGridViewIconData) return;

            var path = AssetDatabase.GetAssetPath(tileGridViewIconData);

            EditorPrefs.SetString(Key, path);
        }

        private void ResetView()
        {
            cameraPosition = CameraPositionDefault;
            originPoint = OriginPointDefault;
            fieldOfView = FieldOfViewDefault;
            UpdateCameraPosition();
            UpdateCameraFov();
            UpdateTexture();
        }

        private void Undo_undoRedoPerformed()
        {
            UpdateTexture();
        }
    }
}