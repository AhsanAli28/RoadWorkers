using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.CityEditor.Road.Tests
{
    public class TestSceneRoadGenerator : MonoBehaviourBase
    {
        [SerializeField] private RoadParent roadParent;
        [SerializeField] private Transform lightParent;
        [SerializeField] private bool pedestrianSupported;
        [SerializeField][Range(0.1f, 500f)] private float createOffset = 10f;
        [SerializeField][Range(1, 100)] private int squareCount = 5;
        [SerializeField] private bool addGraphicsRoad = true;
        [Expandable][SerializeField] private RoadGeneratorPrefabContainer roadGeneratorPrefabContainer;
        [SerializeField] private List<RoadSegmentCreator> createdSegments;
        [SerializeField] private List<GameObject> additionalObjects = new List<GameObject>();

        [Button]
        public void UpdateExistSegments()
        {
            if (roadParent == null)
            {
                roadParent = ObjectUtils.FindObjectsOfType<RoadParent>().Where(a => a.gameObject.scene == gameObject.scene).FirstOrDefault();
            }

            if (lightParent == null)
            {
                lightParent = GameObject.Find($"{roadGeneratorPrefabContainer.LightParentName}")?.transform ?? null;
            }

            createdSegments = ObjectUtils.FindObjectsOfType<RoadSegmentCreator>().Where(a => a.gameObject.scene == gameObject.scene).ToList();
            EditorSaver.SetObjectDirty(this);
        }

        [Button]
        public void Create()
        {
            Clear();

            if (!roadParent)
            {
                UnityEngine.Debug.Log("TestSceneRoadGenerator. Assign road parent");
                return;
            }

            for (int x = 0; x < squareCount; x++)
            {
                for (int y = 0; y < squareCount; y++)
                {
                    var segment = CreateSegment(x, y);
                    createdSegments.Add(segment);
                }
            }

            roadParent.ConnectSegments();

            if (pedestrianSupported)
            {
                roadParent.ConnectPedestrianNodes();
            }
            else
            {
                var pedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>();

                foreach (var pedestrianNode in pedestrianNodes)
                {
                    pedestrianNode.gameObject.SetActive(false);
                }
            }

            roadParent.BakePathData();

            AddStraightRoads();
        }

        private void AddStraightRoads()
        {
            if (!addGraphicsRoad)
            {
                return;
            }

            for (int i = 0; i < createdSegments.Count; i++)
            {
                var nodes = createdSegments[i].trafficLightCrossroad.TrafficNodes;

                for (int j = 0; j < nodes.Count; j++)
                {
                    var node = nodes[j];

                    if (node.ExternalLanes.Count == 0)
                    {
                        UnityEngine.Debug.Log($"{node.TrafficLightCrossroad.name} 0 external lanes");
                        continue;
                    }

                    var dot1 = Vector3.Dot(node.transform.forward, Vector3.forward);
                    var dot2 = Vector3.Dot(node.transform.forward, Vector3.right);

                    if (dot1 <= 0.9f && dot2 <= 0.9f)
                    {
                        continue;
                    }

                    var externalLanes = node.ExternalLanes[0];

                    var connectedNode = externalLanes.paths[0].ConnectedTrafficNode;

                    var sourceLaneLeftPoint = node.GetLanePosition(0, true);
                    var sourceLaneRightPoint = node.GetLanePosition(0);

                    var sourcePoint = (sourceLaneLeftPoint + sourceLaneRightPoint) / 2;

                    var connectedRightpoint = connectedNode.GetLanePosition(0);
                    var connectedLeftpoint = connectedNode.GetLanePosition(0, true);

                    var connectedPoint = (connectedRightpoint + connectedLeftpoint) / 2;

                    var dir = Vector3.Normalize(connectedPoint - sourcePoint);
                    var distance = Vector3.Distance(sourcePoint, connectedPoint);

                    var dot = Mathf.Abs(Vector3.Dot(dir, Vector3.right));

                    var position = (sourcePoint + connectedPoint) / 2;
                    var rotation = Quaternion.identity;

                    if (dot < 0.9f)
                    {
                        rotation = Quaternion.Euler(0, 90, 0);
                    }

                    var meshStraightPrefab = GetGraphics(SegmentType.StraightRoad);

                    var meshStraight = Instantiate(meshStraightPrefab, position, rotation, transform);

                    meshStraight.transform.localScale = new Vector3(distance - roadGeneratorPrefabContainer.StraightRoadOffset, 1, 1);

                    additionalObjects.Add(meshStraight.gameObject);
                }
            }
        }

        private RoadSegmentCreator CreateSegment(int x, int y)
        {
            var edgeIndex = squareCount - 1;

            SegmentType segmentType = SegmentType.Crossroad;
            int rotationIndex = 0;

            if (x == 0 && y == 0)
            {
                segmentType = SegmentType.TurnRoad;
                rotationIndex = 0;
            }
            if (x == 0 && y == edgeIndex)
            {
                segmentType = SegmentType.TurnRoad;
                rotationIndex = 1;
            }
            if (x == edgeIndex && y == edgeIndex)
            {
                segmentType = SegmentType.TurnRoad;
                rotationIndex = 2;
            }
            if (x == edgeIndex && y == 0)
            {
                segmentType = SegmentType.TurnRoad;
                rotationIndex = 3;
            }

            if (segmentType == SegmentType.Crossroad)
            {
                if (x == edgeIndex)
                {
                    segmentType = SegmentType.CrossRoadT;
                    rotationIndex = 0;
                }
                if (y == 0)
                {
                    segmentType = SegmentType.CrossRoadT;
                    rotationIndex = 1;
                }
                if (x == 0)
                {
                    segmentType = SegmentType.CrossRoadT;
                    rotationIndex = 2;
                }
                if (y == edgeIndex)
                {
                    segmentType = SegmentType.CrossRoadT;
                    rotationIndex = 3;
                }
            }

            var prefab = GetPrefab(segmentType);
            var position = (new Vector3(x, 0, y) - new Vector3((float)squareCount / 2, 0, (float)squareCount / 2)) * createOffset + new Vector3(createOffset, 0, createOffset) / 2;
            var rotation = Quaternion.Euler(0, rotationIndex * 90 + GetAngle(segmentType), 0);

            var road = Instantiate(prefab, position, rotation, roadParent.transform);

            if (addGraphicsRoad)
            {
                var meshPrefab = GetGraphics(segmentType);
                var meshRoad = Instantiate(meshPrefab, position, rotation, transform);

                additionalObjects.Add(meshRoad.gameObject);
            }

            return road;
        }

        private RoadSegmentCreator GetPrefab(SegmentType segmentType)
        {
            var data = roadGeneratorPrefabContainer.GeneratorPrefabData;

            if (data.TryGetValue(segmentType, out var prefab))
            {
                return prefab.Prefab;
            }

            return null;
        }

        private float GetAngle(SegmentType segmentType)
        {
            var data = roadGeneratorPrefabContainer.GeneratorPrefabData;

            if (data.TryGetValue(segmentType, out var prefab))
            {
                return prefab.AngleOffset;
            }

            return 0;
        }

        private MeshRenderer GetGraphics(SegmentType segmentType)
        {
            var data = roadGeneratorPrefabContainer.GeneratorPrefabData;

            if (data.TryGetValue(segmentType, out var prefab))
            {
                return prefab.RoadViewPrefab;
            }

            return null;
        }

        private void Clear()
        {
            createdSegments.DestroyGameObjects();
            additionalObjects.DestroyGameObjects();

            if (lightParent != null)
            {
                var transforms = lightParent.GetComponentsInChildren<Transform>().Where(a => a.transform.parent == lightParent).ToList();
                transforms.DestroyGameObjects();
            }
        }
    }
}
