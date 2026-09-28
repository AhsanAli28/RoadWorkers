#if UNITY_EDITOR && FCG
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    [CustomEditor(typeof(FCGPrefabContainer))]
    public class FCGPrefabContainerEditor : Editor
    {
        private FCGPrefabContainer fCGPrefabContainer;
        private FCGSceneGenerator fCGSceneGenerator;
        private PrefabStage prefabStage;
        private ReorderableList segmentList;
        private Collider[] colliders;
        private static Collider selectedCollider;
        private static List<RoadSegmentCreator> blockSegments = new List<RoadSegmentCreator>();
        private RaycastHit[] hits = new RaycastHit[100];

        private void OnEnable()
        {
            fCGPrefabContainer = (FCGPrefabContainer)target;
            fCGSceneGenerator = ObjectUtils.FindObjectOfType<FCGSceneGenerator>();

            PrefabStage.prefabStageOpened += PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing += PrefabStage_prefabStageClosing;
            SceneView.duringSceneGui += SceneView_duringSceneGui;

            prefabStage = EditorExtension.GetCurrentPrefabStage();

            InitList();
            Init();
        }

        private void OnDisable()
        {
            PrefabStage.prefabStageOpened -= PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing -= PrefabStage_prefabStageClosing;
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(fCGPrefabContainer.LocalBlockPrefabs)));

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(fCGPrefabContainer.ShowSceneBinding)));

            var savePathProp = serializedObject.FindProperty(nameof(fCGPrefabContainer.PrefabSavePath));
            bool selectPath = false;

            if (fCGPrefabContainer.ShowSceneBinding)
            {
                if (prefabStage == null)
                {
                    EditorGUILayout.HelpBox("Open prefab stage with FCG block prefab to start prefab binding", MessageType.Info);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(fCGPrefabContainer.GroundLayer)));

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(savePathProp);

                if (GUILayout.Button("+", GUILayout.Width(25f)))
                {
                    selectPath = true;
                }

                EditorGUILayout.EndHorizontal();

                selectedCollider = EditorGUILayout.ObjectField("Selected Collider", selectedCollider, typeof(Collider), true) as Collider;

                if (selectedCollider)
                {
                    EditorGUILayout.LabelField("Block segments:", EditorStyles.boldLabel);
                    segmentList.DoLayoutList();
                }

                GUI.enabled = selectedCollider != null;

                if (GUILayout.Button("Create Block Prefab"))
                {
                    CreatePrefab();
                }

                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();

            if (selectPath)
            {
                var path = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select save prefab path", savePathProp.stringValue);

                if (!string.IsNullOrEmpty(path) && savePathProp.stringValue != path)
                {
                    savePathProp.stringValue = path;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignCollider(Collider collider)
        {
            selectedCollider = collider;
            PopulateList();
            InitList();
            Repaint();
        }

        private void CreatePrefab()
        {
            if (selectedCollider == null)
            {
                UnityEngine.Debug.Log("selectedCollider null");
                return;
            }

            if (blockSegments.Count == 0)
                return;

            var prefabName = $"{selectedCollider.name}_segment";
            var segmentParent = new GameObject(prefabName);
            EditorSceneManager.MoveGameObjectToScene(segmentParent, selectedCollider.gameObject.scene);

            var roadParent = GetParent().transform.GetChild(0);
            segmentParent.transform.SetParent(roadParent);
            segmentParent.transform.position = selectedCollider.transform.position;
            segmentParent.transform.rotation = selectedCollider.transform.rotation;

            var fcgPrefab = segmentParent.AddComponent<FCGPrefab>();

            fcgPrefab.BindingName = selectedCollider.name;
            EditorSaver.SetObjectDirty(fcgPrefab);

            foreach (var segment in blockSegments)
            {
                segment.transform.SetParent(segmentParent.transform);
            }

            var savePath = AssetDatabaseExtension.GetPrefabSavePath(fCGPrefabContainer.PrefabSavePath, prefabName);

            var prefab = PrefabUtility.SaveAsPrefabAsset(segmentParent, savePath, out var success);

            if (prefab && success)
            {
                PrefabExtension.EditPrefab(prefab, (prefabParent) =>
                {
                    var fcgPrefabAsset = prefabParent.GetComponent<FCGPrefab>();
                    fcgPrefabAsset.InnerSegments = fcgPrefabAsset.GetComponentsInChildren<RoadSegmentCreator>().ToList();
                });

                fCGPrefabContainer.LocalBlockPrefabs.Add(prefab.GetComponent<FCGPrefab>());
                EditorSaver.SetObjectDirty(fCGPrefabContainer);

                UnityEngine.Debug.Log($"Prefab '{savePath}' saved");
            }
        }

        private void PopulateList()
        {
            blockSegments.Clear();

            PhysicsScene scene = GetScene();

            const float distance = 200f;
            var count = scene.SphereCast(GetColliderPosition(selectedCollider) - new Vector3(0, 5f), distance, Vector3.up, hits, distance, 1 << LayerMask.NameToLayer(ProjectConstants.TRAFFIC_NODE_LAYER_NAME), QueryTriggerInteraction.Collide);

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    var node = hit.collider.GetComponent<TrafficNode>();

                    if (node)
                    {
                        var creator = GetCreator(node);
                        Create(creator);
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log("Ground not found");
            }
        }

        private void Create(RoadSegmentCreator creator)
        {
            if (blockSegments.Contains(creator) || !CreatorBelongsSurface(creator))
                return;

            blockSegments.Add(creator);

            var list = new List<RoadSegmentCreator>();

            GetConnectedSegments(creator, list);

            foreach (var item in list)
            {
                Create(item);
            }
        }

        private PhysicsScene GetScene()
        {
            return selectedCollider.gameObject.scene.GetPhysicsScene();
        }

        private void GetConnectedSegments(RoadSegmentCreator creator, List<RoadSegmentCreator> list)
        {
            foreach (var item in creator.CreatedTrafficNodes)
            {
                if (item == null) continue;

                item.IterateExternalPaths(path =>
                {
                    if (path.ConnectedTrafficNode)
                    {
                        list.TryToAdd(GetCreator(path.ConnectedTrafficNode));
                    }
                });
            }
        }

        private RoadSegmentCreator GetCreator(TrafficNode node) => node.TrafficLightCrossroad.GetComponent<RoadSegmentCreator>();

        private bool CreatorBelongsSurface(RoadSegmentCreator creator)
        {
            return NodeBelongsSurface(creator.CreatedTrafficNodes[0]) || NodeBelongsSurface(creator.CreatedTrafficNodes[1]);
        }

        private bool NodeBelongsSurface(TrafficNode node)
        {
            if (node == null) return false;

            var bounds = selectedCollider.bounds;

            bounds.size = new Vector3(bounds.size.x, 2, bounds.size.z);

            var nodeCol = node.GetComponent<BoxCollider>();

            return bounds.Intersects(nodeCol.bounds);
        }

        private void Init()
        {
            if (prefabStage != null)
            {
                GameObject parent = GetParent();
                colliders = FCGUtils.GetSurfaceColliders(parent.transform);
                return;
            }
            else
            {
                if (fCGSceneGenerator)
                {
                    colliders = FCGUtils.GetSurfaceColliders();
                    return;
                }
            }

            colliders = null;
        }

        private GameObject GetParent()
        {
            return prefabStage.scene.GetRootGameObjects()[0];
        }

        private Vector3 GetColliderPosition(Collider collider)
        {
            return collider.bounds.center;
        }

        private void InitList()
        {
            segmentList = new ReorderableList(blockSegments, typeof(RoadSegmentCreator), true, false, true, true);

            segmentList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                blockSegments[index] = EditorGUI.ObjectField(rect, $"Segment {index + 1}", blockSegments[index], typeof(RoadSegmentCreator), true) as RoadSegmentCreator;
            };

            segmentList.onRemoveCallback += (ReorderableList list) =>
            {
                blockSegments.RemoveAt(list.index);
            };

            segmentList.onAddCallback += (ReorderableList list) =>
            {
                blockSegments.Add(null);
            };

            segmentList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
            Repaint();
        }

        private void PrefabStage_prefabStageOpened(PrefabStage prefabStage)
        {
            this.prefabStage = prefabStage;
            Init();
        }

        private void PrefabStage_prefabStageClosing(PrefabStage prefabStage)
        {
            prefabStage = null;
            Init();
        }

        private void SceneView_duringSceneGui(SceneView sceneView)
        {
            if (!fCGPrefabContainer.ShowSceneBinding) return;

            for (int i = 0; i < colliders?.Length; i++)
            {
                var collider = colliders[i];

                if (collider == null) continue;

                Vector3 pos = GetColliderPosition(collider);

                var selected = collider == selectedCollider;

                var text = !selected ? "Select" : "[*]";

                EditorExtension.DrawButton(text, pos, 100, () =>
                {
                    AssignCollider(collider);
                });
            }
        }
    }
}
#endif