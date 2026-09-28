#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class TrafficLightHandlerEditorExtension
    {
        private const float EditButtonWidth = 50f;
        private const float DottedLineSize = 5f;
        private const float TrafficNodeBoundsSize = 2f;
        private const string DisconnectButtonText = "-";

        public static void AddList(TrafficLightHandler trafficLightHandler, List<TrafficLightFrameBase> trafficLightFrames)
        {
            for (int i = 0; i < trafficLightFrames?.Count; i++)
            {
                trafficLightHandler.AddCustomTrafficLight(trafficLightFrames[i]);
            }
        }

        public static void RemoveList(TrafficLightHandler trafficLightHandler, List<TrafficLightFrameBase> trafficLightFrames)
        {
            for (int i = 0; i < trafficLightFrames?.Count; i++)
            {
                trafficLightHandler.RemoveTrafficLight(trafficLightFrames[i]);
            }
        }

        public static bool Contains(TrafficLightHandler trafficLightHandler, List<TrafficLightFrameBase> trafficLightFrames)
        {
            bool contain = false;

            for (int i = 0; i < trafficLightFrames?.Count; i++)
            {
                contain = trafficLightHandler.ContainsTrafficLight(trafficLightFrames[i]);

                if (!contain)
                {
                    return contain;
                }
            }

            return contain;
        }

        public static void DrawLightConnections(TrafficLightHandler trafficLightHandler, TrafficLightHandler.ShowLightConnectionType showLightConnectionType, Color handlesColor, bool drawDisconnectButton = false)
        {
            var handlerPosition = trafficLightHandler.transform.position;
            Handles.color = handlesColor;

            var handlerCubeSize = Vector3.one * 4;

            Handles.DrawWireCube(handlerPosition, handlerCubeSize);

            if (showLightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.Light))
            {
                var childLights = trafficLightHandler.ChildLights;
                var customLights = trafficLightHandler.CustomLights;

                DrawLightConnection(trafficLightHandler, childLights, handlerPosition, drawDisconnectButton);
                DrawLightConnection(trafficLightHandler, customLights, handlerPosition, drawDisconnectButton);
            }

            if (showLightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.TrafficNode))
            {
                for (int i = 0; i < trafficLightHandler.TrafficNodes?.Length; i++)
                {
                    TrafficNode trafficNode = trafficLightHandler.TrafficNodes[i];
                    DrawTrafficNodeConnection(trafficLightHandler, handlerPosition, trafficNode);
                }
            }
        }

        public static void DrawPedestrianNodeConnection(PedestrianNode pedestrianNode)
        {
            if (pedestrianNode.RelatedTrafficLightHandler != null)
            {
                var pedestrianNodePos = pedestrianNode.transform.position;

                var handlerPosition = pedestrianNode.RelatedTrafficLightHandler.transform.position;

                Handles.DrawLine(handlerPosition, pedestrianNodePos);

                Vector3 buttonPosition = (handlerPosition + pedestrianNodePos) / 2;

                System.Action removeCallback = () =>
                {
                    pedestrianNode.RelatedTrafficLightHandler = null;
                    EditorSaver.SetObjectDirty(pedestrianNode);
                };

                EditorExtension.DrawButton(DisconnectButtonText, buttonPosition, EditButtonWidth, removeCallback, centralizeGuiAlign: true);
            }
        }

        public static void DrawLightObjectBounds(Vector3 position, float sizeMult = 1f)
        {
            var size = Vector3.one * sizeMult;
            Handles.DrawWireCube(position, size);
        }

        private static void DrawDisconnectLightButton(TrafficLightHandler trafficLightHandler, TrafficLightFrameBase trafficLightObject, Vector3 buttonPosition)
        {
            System.Action removeCallback = () => { trafficLightHandler.RemoveTrafficLight(trafficLightObject); };
            EditorExtension.DrawButton(DisconnectButtonText, buttonPosition, EditButtonWidth, removeCallback, centralizeGuiAlign: true);
        }

        private static void DrawLightConnection(TrafficLightHandler trafficLightHandler, List<TrafficLightFrameBase> lights, Vector3 handlerPosition, bool drawDisconnectButton = false)
        {
            for (int i = 0; i < lights?.Count; i++)
            {
                if (lights[i] == null || !lights[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                var lightPosition = lights[i].transform.position;

                Handles.DrawLine(handlerPosition, lightPosition);

                DrawLightObjectBounds(lightPosition);

                if (drawDisconnectButton)
                {
                    Vector3 buttonPosition = (handlerPosition + lightPosition) / 2;
                    DrawDisconnectLightButton(trafficLightHandler, lights[i], buttonPosition);
                }
            }
        }

        private static void DrawTrafficNodeConnection(TrafficLightHandler trafficLightHandler, Vector3 handlerPosition, TrafficNode trafficNode)
        {
            if (trafficNode != null)
            {
                var trafficNodePosition = trafficNode.transform.position;
                DrawLightObjectBounds(trafficNodePosition, TrafficNodeBoundsSize);

                var nodeIsConnected = trafficLightHandler.Connected(trafficNode);

                if (nodeIsConnected)
                {
                    Handles.DrawLine(handlerPosition, trafficNodePosition);

                    Vector3 buttonPosition = (handlerPosition + trafficNodePosition) / 2;
                    DrawDisconnectTrafficNodeButton(trafficLightHandler, trafficNode, buttonPosition);
                }
                else
                {
                    Handles.DrawDottedLine(handlerPosition, trafficNodePosition, DottedLineSize);
                }
            }
        }

        private static void DrawDisconnectTrafficNodeButton(TrafficLightHandler trafficLightHandler, TrafficNode trafficNode, Vector3 buttonPosition)
        {
            System.Action removeCallback = () => { trafficLightHandler.TryToRemoveNode(trafficNode); };
            EditorExtension.DrawButton(DisconnectButtonText, buttonPosition, EditButtonWidth, removeCallback, centralizeGuiAlign: true);
        }
    }
}
#endif