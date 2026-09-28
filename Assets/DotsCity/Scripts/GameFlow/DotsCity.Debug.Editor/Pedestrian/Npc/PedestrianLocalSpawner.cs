using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianLocalSpawner : MonoBehaviourBase
    {
        #region Helper type

        [System.Serializable]
        public class SpawnInfo
        {
            [HideIf(nameof(AppIsRunning))]
            public PedestrianNode PedestrianNode;
            public Entity PedestrianNodeEntity;

            [Range(0, 100)] public int spawnCount;

            public SpawnInfo(PedestrianNode pedestrianNode)
            {
                PedestrianNode = pedestrianNode;
                PedestrianNodeEntity = Entity.Null;
                spawnCount = 1;
            }
        }

        #endregion

        #region Serialized Variables

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianTestScenes.html#pedestrianlocalspawner")]
        [SerializeField] private string link;

        [SerializeField] private bool spawnOnPlay;
        public bool ShowSceneButtons = true;
        public bool HighLightAddedNodes = true;
        [Range(0, 10f)] public float highLightRadius = 1f;

        [SerializeField] private bool showChildNodesOnly = true;
        [SerializeField] private List<SpawnInfo> spawnInfos = new List<SpawnInfo>();

        #endregion

        #region Variables

        private PedestrianEntitySpawnerSystem pedestrianSpawnerSystem;
        private bool isInitialized;

        #endregion

        #region Properties

        public bool ShowChildNodesOnly { get => showChildNodesOnly; set => showChildNodesOnly = value; }
        public List<SpawnInfo> SpawnInfos { get => spawnInfos; }
        private bool AppIsRunning => Application.isPlaying;

        #endregion

        #region Unity lifecycle

        private void OnDestroy()
        {
            PedestrianEntitySpawnerSystem.OnInitialized -= PedestrianEntitySpawnerSystem_OnInitialized;
        }

        class PedestrianDebugLocalSpawnerBaker : Baker<PedestrianLocalSpawner>
        {
            public override void Bake(PedestrianLocalSpawner authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                var spawnInfos = authoring.spawnInfos;

                NativeArray<Entity> nodeEntities = new NativeArray<Entity>(spawnInfos.Count, Allocator.Temp);

                for (int i = 0; i < spawnInfos.Count; i++)
                {
                    SpawnInfo spawnInfo = spawnInfos[i];

                    if (spawnInfo.PedestrianNode != null)
                    {
                        nodeEntities[i] = GetEntity(spawnInfo.PedestrianNode.gameObject, TransformUsageFlags.Dynamic);
                    }
                }

                AddComponent(entity, new PedestrianLocalSpawnerBakingData()
                {
                    LocalSpawnerInstanceId = authoring.gameObject.GetInstanceID(),
                    PedestrianNodeEntities = nodeEntities
                });
            }
        }

        #endregion

        #region Public Methods

        public void Spawn()
        {
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.Log("Enter playmode!");
                return;
            }

            pedestrianSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();

            for (int i = 0; i < spawnInfos?.Count; i++)
            {
                if (spawnInfos[i].PedestrianNodeEntity != Entity.Null)
                {
                    pedestrianSpawnerSystem.Spawn(true, spawnInfos[i].PedestrianNodeEntity, spawnInfos[i].spawnCount);
                }
                else
                {
                    UnityEngine.Debug.Log($"PedestrianLocalSpawner {name} LocalIndex '{i}' entity not initialized");
                }
            }
        }

        public void Spawn(int index)
        {
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.Log("Enter playmode!");
                return;
            }

            pedestrianSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();
            pedestrianSpawnerSystem.Spawn(true, spawnInfos[index].PedestrianNodeEntity, spawnInfos[index].spawnCount);
        }

        public void AddNode(PedestrianNode pedestrianNode)
        {
            if (!HasNode(pedestrianNode))
            {
                spawnInfos.Add(new SpawnInfo(pedestrianNode));
            }
        }

        public void RemoveNode(PedestrianNode pedestrianNode)
        {
            if (HasNode(pedestrianNode))
            {
                var spawnInfo = GetSpawnInfo(pedestrianNode);
                spawnInfos.Remove(spawnInfo);
            }
        }

        public bool HasNode(PedestrianNode pedestrianNode)
        {
            var spawnInfo = spawnInfos.FirstOrDefault(item => item.PedestrianNode == pedestrianNode);

            return spawnInfo != null;
        }

        public void InitEntity(Entity nodeEntity, int index)
        {
            spawnInfos[index].PedestrianNodeEntity = nodeEntity;

            Initialize();
        }

        #endregion

        #region Private Methods

        private SpawnInfo GetSpawnInfo(PedestrianNode pedestrianNode)
        {
            var spawnInfo = spawnInfos.FirstOrDefault(item => item.PedestrianNode == pedestrianNode);

            return spawnInfo;
        }

        private void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            pedestrianSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();
            RegisterSpawnEvent();
        }

        private void RegisterSpawnEvent()
        {
            if (!pedestrianSpawnerSystem.IsInitialized)
            {
                PedestrianEntitySpawnerSystem.OnInitialized += PedestrianEntitySpawnerSystem_OnInitialized;
            }
            else
            {
                PedestrianEntitySpawnerSystem_OnInitialized();
            }
        }

        #endregion

        #region Events

        private void PedestrianEntitySpawnerSystem_OnInitialized()
        {
            PedestrianEntitySpawnerSystem.OnInitialized -= PedestrianEntitySpawnerSystem_OnInitialized;

            if (spawnOnPlay)
            {
                Spawn();
            }
        }

        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PedestrianLocalSpawner))]
    public class PedestrianDebugLocalSpawnerEditor : Editor
    {
        private PedestrianNode[] pedestrianNodes;
        private PedestrianLocalSpawner pedestrianSpawner;

        private void OnEnable()
        {
            pedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>();
            pedestrianSpawner = target as PedestrianLocalSpawner;
        }

        private void OnDisable()
        {
            pedestrianNodes = null;
        }

        public override void OnInspectorGUI()
        {
            var controller = target as PedestrianLocalSpawner;

            base.OnInspectorGUI();

            if (GUILayout.Button("Spawn"))
            {
                controller.Spawn();
            }
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (pedestrianSpawner.ShowSceneButtons)
            {
                for (int i = 0; i < pedestrianNodes?.Length; i++)
                {
                    var pedestrianNode = pedestrianNodes[i];
                    var labelPosition = pedestrianNode.transform.position;

                    bool contains = pedestrianSpawner.HasNode(pedestrianNode);

                    if (!contains)
                    {
                        bool showNode = true;

                        if (pedestrianSpawner.ShowChildNodesOnly)
                        {
                            var nodeParent = pedestrianNode.transform.parent;
                            showNode = false;

                            while (nodeParent != null)
                            {
                                showNode = nodeParent == pedestrianSpawner.transform;

                                if (showNode)
                                {
                                    break;
                                }

                                nodeParent = nodeParent.parent;
                            }
                        }

                        if (showNode)
                        {
                            System.Action addCallback = () =>
                            {
                                pedestrianSpawner.AddNode(pedestrianNode);
                            };

                            EditorExtension.DrawButton("+", labelPosition, 35f, addCallback);
                        }
                    }
                    else
                    {
                        System.Action removeCallback = () =>
                        {
                            pedestrianSpawner.RemoveNode(pedestrianNode);
                        };

                        EditorExtension.DrawButton("-", labelPosition, 35f, removeCallback);
                    }
                }
            }

            if (pedestrianSpawner.HighLightAddedNodes)
            {
                for (int i = 0; i < pedestrianSpawner.SpawnInfos?.Count; i++)
                {
                    var node = pedestrianSpawner.SpawnInfos[i].PedestrianNode;

                    if (node)
                    {
                        Handles.DrawWireDisc(node.transform.position, Vector3.up, pedestrianSpawner.highLightRadius);
                    }
                }
            }

        }
    }
#endif
}
