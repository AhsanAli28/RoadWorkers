#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CustomEditor(typeof(TrafficLightHandler))]
    public class TrafficLightHandlerEditor : Editor
    {
        private const float EditButtonWidth = 50f;
        private const string ConnectButtonText = "+";
        private const string DisconnectButtonText = "-";

        private ReorderableList reordableList;
        private TrafficLightObject[] trafficLightObjects;
        private TrafficNode[] trafficNodes;
        private PedestrianNode[] pedestrianNodes;
        private List<PedestrianNode> connectedPedestrianNodes;

        private void OnEnable()
        {
            TrafficLightHandler handler = (TrafficLightHandler)target;

            reordableList = LightStateDrawer.DrawList("lightStates", serializedObject, handler.lightStates, AddItem, removeCallback: RemoveItem);

            trafficLightObjects = ObjectUtils.FindObjectsOfType<TrafficLightObject>().ToArray();
            trafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>().ToArray();
            pedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().ToArray();
            connectedPedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().Where(a => a.RelatedTrafficLightHandler == handler).ToList();
        }

        public void AddItem(object obj)
        {
            var lightStateAddData = (LightStateAddData)obj;
            LightState lightState = lightStateAddData.LightState;

            TrafficLightHandler handler = (TrafficLightHandler)target;

            handler.lightStates.Add(new LightStateInfo() { LightState = lightState });

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveItem(ReorderableList list)
        {
            var trafficLightHandler = target as TrafficLightHandler;
            int i = list.index;

            trafficLightHandler.lightStates.RemoveAt(i);

            EditorSaver.SetObjectDirty(trafficLightHandler);
        }

        public override void OnInspectorGUI()
        {
            var trafficLightHandler = target as TrafficLightHandler;

            base.OnInspectorGUI();

            reordableList.DoLayoutList();

            if (GUILayout.Button("Find Child Lights"))
            {
                trafficLightHandler.FindChildLights();
            }
        }

        private void OnSceneGUI()
        {
            var trafficLightHandler = target as TrafficLightHandler;

            if (trafficLightHandler.ShowWorldTrafficLights)
            {
                ShowWorldLights(trafficLightHandler, trafficLightHandler.VisibleLightConnectionType);
            }

            if (trafficLightHandler.ShowLightConnection)
            {
                TrafficLightHandlerEditorExtension.DrawLightConnections(trafficLightHandler, trafficLightHandler.VisibleLightConnectionType, Color.white);
                DrawConnectedPedestrianNodeConnections(trafficLightHandler);
            }
        }

        private void ShowWorldLights(TrafficLightHandler trafficLightHandler, TrafficLightHandler.ShowLightConnectionType showLightConnectionType)
        {
            if (showLightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.Light))
            {
                for (int i = 0; i < trafficLightObjects.Length; i++)
                {
                    var trafficLightObject = trafficLightObjects[i];

                    bool shouldShow = trafficLightObject.HasLightIndex(trafficLightHandler.RelatedLightIndex);

                    if (shouldShow)
                    {
                        var trafficLightFrames = trafficLightObject.GetLightFrames(trafficLightHandler.RelatedLightIndex);

                        bool contains = TrafficLightHandlerEditorExtension.Contains(trafficLightHandler, trafficLightFrames);

                        var trafficLightObjectPosition = trafficLightObject.transform.position;

                        if (!contains)
                        {
                            System.Action addCallback = () => { TrafficLightHandlerEditorExtension.AddList(trafficLightHandler, trafficLightFrames); };
                            EditorExtension.DrawButton(ConnectButtonText, trafficLightObjectPosition, EditButtonWidth, addCallback);
                        }
                        else
                        {
                            System.Action removeCallback = () => { TrafficLightHandlerEditorExtension.RemoveList(trafficLightHandler, trafficLightFrames); };
                            EditorExtension.DrawButton(DisconnectButtonText, trafficLightObjectPosition, EditButtonWidth, removeCallback);
                        }

                        TrafficLightHandlerEditorExtension.DrawLightObjectBounds(trafficLightObjectPosition);
                    }
                }
            }

            if (showLightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.TrafficNode))
            {
                for (int i = 0; i < trafficNodes.Length; i++)
                {
                    var trafficNode = trafficNodes[i];

                    bool nodeIsConnected = trafficLightHandler.Connected(trafficNode);
                    var trafficLightObjectPosition = trafficNode.transform.position;

                    if (!nodeIsConnected)
                    {
                        System.Action addCallback = () =>
                        {
                            trafficLightHandler.TrafficLightCrossroad.RemoveNode(trafficNode);
                            trafficLightHandler.AddNode(trafficNode);
                        };

                        EditorExtension.DrawButton(ConnectButtonText, trafficLightObjectPosition, EditButtonWidth, addCallback);
                    }
                    else
                    {
                        System.Action removeCallback = () =>
                        {
                            trafficLightHandler.TrafficLightCrossroad.RemoveNode(trafficNode);
                        };

                        EditorExtension.DrawButton(DisconnectButtonText, trafficLightObjectPosition, EditButtonWidth, removeCallback);
                    }
                }
            }

            if (showLightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.PedestrianNode))
            {
                for (int i = 0; i < pedestrianNodes.Length; i++)
                {
                    PedestrianNode pedestrianNode = pedestrianNodes[i];

                    bool nodeIsConnected = connectedPedestrianNodes.Contains(pedestrianNode);

                    if (nodeIsConnected && pedestrianNode.RelatedTrafficLightHandler != trafficLightHandler)
                    {
                        connectedPedestrianNodes.TryToRemove(pedestrianNode);
                        break;
                    }

                    var trafficLightObjectPosition = pedestrianNode.transform.position;

                    if (!nodeIsConnected)
                    {
                        System.Action addCallback = () =>
                        {
                            pedestrianNode.RelatedTrafficLightHandler = trafficLightHandler;
                            connectedPedestrianNodes.TryToAdd(pedestrianNode);
                            EditorSaver.SetObjectDirty(pedestrianNode);
                        };

                        EditorExtension.DrawButton(ConnectButtonText, trafficLightObjectPosition, EditButtonWidth, addCallback);
                    }
                    else
                    {
                        TrafficLightHandlerEditorExtension.DrawPedestrianNodeConnection(pedestrianNode);
                    }
                }
            }
        }

        private void DrawConnectedPedestrianNodeConnections(TrafficLightHandler trafficLightHandler)
        {
            if (!trafficLightHandler.ShowWorldTrafficLights)
            {
                for (int i = 0; i < connectedPedestrianNodes?.Count; i++)
                {
                    if (connectedPedestrianNodes[i].RelatedTrafficLightHandler == trafficLightHandler)
                    {
                        TrafficLightHandlerEditorExtension.DrawPedestrianNodeConnection(connectedPedestrianNodes[i]);
                    }
                }
            }
        }
    }
}
#endif