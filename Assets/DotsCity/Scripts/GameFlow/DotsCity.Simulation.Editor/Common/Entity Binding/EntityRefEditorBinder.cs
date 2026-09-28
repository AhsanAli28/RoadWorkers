#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.DotsCity.Simulation.Binding
{
    public static class EntityRefEditorBinder
    {
        private struct BindingData
        {
            public int Id;
            public Vector3 Position;
            public SceneEntityBindingType SceneEntityBindingType;
        }

        public enum SceneEntityBindingType { TrafficNode, PedestrianNode }

        private const float ButtonScreenSize = 30f;
        private const float SceneCubeSize = 1f;

        private static List<BindingData> bindingDatas = new List<BindingData>();
        private static EntityWeakRef selectedEntityRef;

        public static EntityWeakRef SelectedEntityRef
        {
            get => selectedEntityRef;
            set
            {
                selectedEntityRef = value;

                if (value != null)
                {
                    Selection.selectionChanged -= Selection_selectionChanged;
                    Selection.selectionChanged += Selection_selectionChanged;
                    Init(true);
                }
            }
        }

        public static Object SourceObject { get; set; }
        public static event Action<EntityWeakRef> OnBind = delegate { };

        public static SceneEntityBindingType CurrentSceneEntityBindingType { get; set; }

        private static bool IsInitialized { get; set; }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        public static void Init(bool force = false)
        {
            if (IsInitialized && !force)
                return;

            bindingDatas.Clear();

            var trafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>();

            foreach (var trafficNode in trafficNodes)
            {
                trafficNode.IterateAllLanes((lane, index, ext) =>
                {
                    bindingDatas.Add(new BindingData()
                    {
                        Id = lane.UniqueID,
                        Position = trafficNode.GetLanePosition(index, ext),
                        SceneEntityBindingType = SceneEntityBindingType.TrafficNode
                    });
                }, true);

                if (trafficNode.HasSubNodes)
                {
                    trafficNode.IterateAllPaths(path =>
                    {
                        path.IterateWaypoints(pathNode =>
                        {
                            if (pathNode.SpawnNode)
                            {
                                bindingDatas.Add(new BindingData()
                                {
                                    Id = pathNode.UniqueID,
                                    Position = pathNode.transform.position,
                                    SceneEntityBindingType = SceneEntityBindingType.TrafficNode
                                });
                            }
                        });
                    }, true);
                }
            }

            var pedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>();

            foreach (var pedestrianNode in pedestrianNodes)
            {
                bindingDatas.Add(new BindingData()
                {
                    Id = pedestrianNode.UniqueID,
                    Position = pedestrianNode.transform.position,
                    SceneEntityBindingType = SceneEntityBindingType.PedestrianNode
                });
            }

            IsInitialized = true;
        }

        private static void SceneView_duringSceneGui(SceneView obj)
        {
            if (SelectedEntityRef == null)
                return;

            if (!IsInitialized)
                Init();

            for (int i = 0; i < bindingDatas.Count; i++)
            {
                if (bindingDatas[i].SceneEntityBindingType != EntityRefEditorBinder.CurrentSceneEntityBindingType)
                    continue;

                var index = i;
                var pos = bindingDatas[i].Position;

                bool hasSelected = SelectedEntityRef != null;
                bool isInitialized = bindingDatas[i].Id != 0;

                bool notSelected = isInitialized && hasSelected && bindingDatas[i].Id != SelectedEntityRef.Id;

                var prevColor = Handles.color;

                Color color = Color.red;

                if (isInitialized)
                {
                    color = notSelected ? Color.yellow : Color.green;
                }

                Handles.color = color;
                Handles.DrawWireCube(pos + new Vector3(0, SceneCubeSize / 2), Vector3.one * SceneCubeSize);

                if (hasSelected && isInitialized)
                {
                    if (notSelected)
                    {
                        EditorExtension.DrawButton("+", pos, ButtonScreenSize, () =>
                        {
                            SelectedEntityRef.Id = bindingDatas[index].Id;
                            EditorSaver.SetObjectDirty(SourceObject);
                            OnBind(SelectedEntityRef);
                        });
                    }
                    else
                    {
                        EditorExtension.DrawButton("-", pos, ButtonScreenSize, () =>
                        {
                            SelectedEntityRef.Id = 0;
                            EditorSaver.SetObjectDirty(SourceObject);
                            OnBind(SelectedEntityRef);
                        });
                    }
                }

                Handles.color = prevColor;
            }
        }

        private static void Selection_selectionChanged()
        {
            Selection.selectionChanged -= Selection_selectionChanged;
            SelectedEntityRef = null;
        }
    }
}
#endif
