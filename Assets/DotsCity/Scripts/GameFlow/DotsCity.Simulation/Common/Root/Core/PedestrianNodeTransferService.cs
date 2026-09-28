using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    public class PedestrianNodeTransferService : MonoBehaviour
    {
        private const float CellHashSize = 0.1f;

        [Serializable]
        public class PedestrianNodeConnection
        {
            public PedestrianNode SceneNode;
            public int SourceHash;
            public Vector3 SourcePosition;
        }

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/structure.html#pedestriannode-transfer-service")]
        [SerializeField] private string link;

        [SerializeField] private List<PedestrianNodeConnection> pedestrianNodeConnections = new List<PedestrianNodeConnection>();
        [SerializeField] private List<PedestrianNode> duplicateNodes = new List<PedestrianNode>();

        public int DuplicateCount => duplicateNodes.Count;

        public IList<PedestrianNode> DuplicateNodes => duplicateNodes.AsReadOnly();

#if UNITY_EDITOR

        private Dictionary<int, PedestrianNode> sceneNodes = new Dictionary<int, PedestrianNode>();
        private Dictionary<int, PedestrianNode> mirroredNodes = new Dictionary<int, PedestrianNode>();
        private List<PedestrianNode> tempBuffer = new List<PedestrianNode>();

        [Button]
        public void ConvertNodes(bool autoLoadNodes = true)
        {
            if (autoLoadNodes)
            {
                LoadSceneNodes();
            }

            RestoreScene();

            var sortedSceneNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().Where(
                a => a.gameObject.scene == gameObject.scene &&
                PrefabUtility.IsPartOfAnyPrefab(a.gameObject) &&
                !PrefabUtility.IsOutermostPrefabInstanceRoot(a.gameObject) &&
                PrefabUtility.GetOutermostPrefabInstanceRoot(a.gameObject).GetComponent<MeshRenderer>() != null);

            foreach (var sceneNode in sortedSceneNodes)
            {
                var sourceHash = GetHash(sceneNode.transform);

                var pedestrianNodeConnection = new PedestrianNodeConnection()
                {
                    SceneNode = sceneNode,
                    SourceHash = sourceHash,
                    SourcePosition = sceneNode.transform.position,
                };

                pedestrianNodeConnections.Add(pedestrianNodeConnection);

                var newNode = Instantiate(sceneNode);

                newNode.transform.position = sceneNode.transform.position;
                newNode.transform.rotation = sceneNode.transform.rotation;

                var mirrorNode = newNode.gameObject.AddComponent<MirrorNode>();
                mirrorNode.SourceNodeHash = sourceHash;

                tempBuffer.Clear();
                tempBuffer.AddRange(sceneNode.AutoConnectedPedestrianNodes);

                foreach (var connectedNode in tempBuffer)
                {
                    connectedNode.RemoveConnection(sceneNode);
                    connectedNode.AddAutoConnection(newNode);
                }

                tempBuffer.Clear();
                tempBuffer.AddRange(sceneNode.DefaultConnectedPedestrianNodes);

                foreach (var connectedNode in tempBuffer)
                {
                    connectedNode.RemoveConnection(sceneNode);
                    connectedNode.AddConnection(newNode);
                }

                sceneNode.gameObject.SetActive(false);
            }

            tempBuffer.Clear();
            mirroredNodes.Clear();
            sceneNodes.Clear();
        }

        [Button]
        public void RestoreScene()
        {
            mirroredNodes.Clear();
            var mirrorNodes = ObjectUtils.FindObjectsOfType<MirrorNode>().ToList();

            foreach (var mirrorNode in mirrorNodes)
            {
                mirroredNodes.Add(mirrorNode.SourceNodeHash, mirrorNode.GetComponent<PedestrianNode>());
            }

            foreach (var pedestrianNodeConnection in pedestrianNodeConnections)
            {
                var sourceNode = pedestrianNodeConnection.SceneNode;
                sourceNode.gameObject.SetActive(true);

                if (!mirroredNodes.ContainsKey(pedestrianNodeConnection.SourceHash))
                {
                    UnityEngine.Debug.Log($"RestoreScene report. PedestrianNode Hash {pedestrianNodeConnection.SourceHash} Position {pedestrianNodeConnection.SourcePosition} not found");
                    continue;
                }

                var tempNode = mirroredNodes[pedestrianNodeConnection.SourceHash];

                tempBuffer.Clear();
                tempBuffer.AddRange(tempNode.AutoConnectedPedestrianNodes);

                foreach (var connectedNode in tempBuffer)
                {
                    connectedNode.RemoveConnection(tempNode);
                    connectedNode.AddAutoConnection(sourceNode);
                }

                tempBuffer.Clear();
                tempBuffer.AddRange(tempNode.DefaultConnectedPedestrianNodes);

                foreach (var connectedNode in tempBuffer)
                {
                    connectedNode.RemoveConnection(tempNode);
                    connectedNode.AddConnection(sourceNode);
                }


                DestroyImmediate(tempNode.gameObject);
            }

            pedestrianNodeConnections.Clear();
            mirrorNodes.DestroyGameObjects();

            EditorSaver.SetObjectDirty(this);
        }

        public bool LoadSceneNodes()
        {
            var allSceneNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().Where(a => a.gameObject.scene == gameObject.scene);

            sceneNodes.Clear();
            duplicateNodes.Clear();

            foreach (var sceneNode in allSceneNodes)
            {
                var sourceHash = GetHash(sceneNode.transform);

                if (sceneNodes.ContainsKey(sourceHash))
                {
                    var existNode = sceneNodes[sourceHash];

                    if (sceneNode.TryToGetMergeNode() != existNode)
                    {
                        duplicateNodes.Add(sceneNode);
                        continue;
                    }

                    sceneNodes.Add(sourceHash + 1, sceneNode);
                }
                else
                {
                    sceneNodes.Add(sourceHash, sceneNode);
                }
            }

            return duplicateNodes.Count == 0;
        }

        private int GetHash(Transform transform) => HashMapHelper.GetHashMapPosition(transform.position, CellHashSize);

#endif
    }
}