using Spirit604.Attributes;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficNodeCrosswalk : MonoBehaviourBase
    {
        [SerializeField] private PedestrianNode pedestrianNode1;
        [SerializeField] private PedestrianNode pedestrianNode2;
        [SerializeField] private Vector3 crossWalkOffset;

        public Vector3 CrossWalkOffset { get => crossWalkOffset; set => crossWalkOffset = value; }

        public bool Enabled => pedestrianNode1 != null && pedestrianNode2 != null && pedestrianNode1.gameObject.activeSelf && pedestrianNode2.gameObject.activeSelf;

        public bool HasConnection => Enabled && pedestrianNode1.HasConnection(pedestrianNode2);

        public void SwitchConnectionState(bool hasConnection, bool recordUndo = false)
        {
            if (pedestrianNode1 == null || pedestrianNode2 == null)
            {
                UnityEngine.Debug.LogError("Assign pedestrian nodes");
                return;
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode1, "Undo Pedestrian Node position");
                UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode2, "Undo Pedestrian Node position");
#endif
            }

            if (!hasConnection)
            {
                pedestrianNode1.RemoveConnection(pedestrianNode2);
            }
            else
            {
                pedestrianNode1.AddConnection(pedestrianNode2);
            }
        }

        public void SetCrosswalkPosition(TrafficNode trafficNode, bool recordUndo = false)
        {
            var newPos1 = trafficNode.transform.position + trafficNode.transform.rotation * GetOffset(trafficNode, -1);

            if (pedestrianNode1.transform.position != newPos1)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode1.transform, "Undo Pedestrian Node position");
#endif
                }

                pedestrianNode1.transform.position = newPos1;
            }

            var newPos2 = trafficNode.transform.position + trafficNode.transform.rotation * GetOffset(trafficNode, 1);

            if (pedestrianNode2.transform.position != newPos2)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode2.transform, "Undo Pedestrian Node position");
#endif
                }

                pedestrianNode2.transform.position = newPos2;
            }
        }

        public void Connect(TrafficNodeCrosswalk other)
        {
            pedestrianNode1.AddConnection(other.pedestrianNode2);
            pedestrianNode2.AddConnection(other.pedestrianNode1);
        }

        public void Disconnect(TrafficNodeCrosswalk other)
        {
            pedestrianNode1.RemoveConnection(other.pedestrianNode2);
            pedestrianNode2.RemoveConnection(other.pedestrianNode1);
        }

        public bool IsConnected(TrafficNodeCrosswalk other)
        {
            return pedestrianNode1.ConnectedTo(other.pedestrianNode2) && pedestrianNode2.ConnectedTo(other.pedestrianNode1);
        }

        public Vector3 GetOffset(TrafficNode trafficNode, int side) => new Vector3(side * (CrossWalkOffset.x + trafficNode.CalculatedRouteWidth), CrossWalkOffset.y, CrossWalkOffset.z);

        public void SetCrosswalkPosition(bool recordUndo = false)
        {
            var trafficNode = GetComponent<TrafficNode>();

            SetCrosswalkPosition(trafficNode, recordUndo);
        }

        public void SwitchEnabledState(bool isEnabled)
        {
            if (pedestrianNode1 && pedestrianNode1.gameObject.activeSelf != isEnabled)
            {
                pedestrianNode1.gameObject.SetActive(isEnabled);
            }

            if (pedestrianNode2 && pedestrianNode2.gameObject.activeSelf != isEnabled)
            {
                pedestrianNode2.gameObject.SetActive(isEnabled);
            }
        }

        public void SetCustomWidth(float width, float height = 0)
        {
            pedestrianNode1.MaxPathWidth = width;
            pedestrianNode2.MaxPathWidth = width;

            if (height != 0)
            {
                pedestrianNode1.Height = height;
                pedestrianNode2.Height = height;
            }

            EditorSaver.SetObjectDirty(this);
        }

        public Vector2 GetSize()
        {
            return new Vector2(pedestrianNode1.MaxPathWidth, pedestrianNode1.Height);
        }

        public NodeShapeType GetShapeType()
        {
            return pedestrianNode1.PedestrianNodeShapeType;
        }

        public void SetType(NodeShapeType type, bool recordUndo = false)
        {
            if (recordUndo)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode1, "Undo Pedestrian Node Settings");
                UnityEditor.Undo.RegisterCompleteObjectUndo(pedestrianNode2, "Undo Pedestrian Node Settings");
#endif
            }

            pedestrianNode1.PedestrianNodeShapeType = type;
            pedestrianNode2.PedestrianNodeShapeType = type;

            EditorSaver.SetObjectDirty(pedestrianNode1);
            EditorSaver.SetObjectDirty(pedestrianNode2);
        }

        public int GetCrosswalkNodeIndex(PedestrianNode node)
        {
            if (pedestrianNode1 == node)
            {
                return 0;
            }
            if (pedestrianNode2 == node)
            {
                return 1;
            }

            return -1;
        }

        public PedestrianNode GetCrosswalkNode(int index)
        {
            switch (index)
            {
                case 0:
                    {
                        return pedestrianNode1;
                    }
                case 1:
                    {
                        return pedestrianNode2;
                    }
            }

            return null;
        }

        [Button]
        public void Connect()
        {
            SwitchConnectionState(true);
        }

        [Button]
        public void Disconnect()
        {
            SwitchConnectionState(false);
        }

        [ShowIf(nameof(Enabled))]
        [Button]
        public void Disable()
        {
            SwitchEnabledState(false);
        }

        [DisableIf(nameof(Enabled))]
        [Button]
        public void Enable()
        {
            SwitchEnabledState(true);
        }

        public TrafficLightHandler GetLight()
        {
            if (pedestrianNode1)
                return pedestrianNode1.RelatedTrafficLightHandler;

            if (pedestrianNode2)
                return pedestrianNode2.RelatedTrafficLightHandler;

            return null;
        }

        public void SetLight(TrafficLightHandler lightHandler)
        {
            if (pedestrianNode1)
            {
                pedestrianNode1.RelatedTrafficLightHandler = lightHandler;
                EditorSaver.SetObjectDirty(pedestrianNode1);
            }

            if (pedestrianNode2)
            {
                pedestrianNode2.RelatedTrafficLightHandler = lightHandler;
                EditorSaver.SetObjectDirty(pedestrianNode2);
            }
        }
    }
}