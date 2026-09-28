using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
    [CustomEditor(typeof(RuntimeSegmentCustom))]
    public class RuntimeSegmentCustomEditor : SharedSettingsEditorBase<RuntimeSegmentCustomEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool ShowConnection = true;
        }

        private RuntimeSegmentCustom runtimeSegmentCustom;

        protected override string SaveKey => "RuntimeSegmentCustomEditor";

        protected override void OnEnable()
        {
            runtimeSegmentCustom = (RuntimeSegmentCustom)target;
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SharedSettings.ShowConnection = EditorGUILayout.Toggle("Show Connection", SharedSettings.ShowConnection);
        }

        private void OnSceneGUI()
        {
            Handles.DrawWireCube(runtimeSegmentCustom.Position, Vector3.one * 10);

            if (SharedSettings.ShowConnection)
            {
                var nodes = runtimeSegmentCustom.Nodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];
                    var connectedExternal = node.ConnectedExternalNode;

                    if (connectedExternal != null)
                    {
                        var color = connectedExternal != node ? Color.green : Color.magenta;

                        var dist = Vector3.SqrMagnitude(connectedExternal.Position - node.Position);

                        if (dist > 1)
                        {
                            color = Color.yellow;
                            const float radius = 1f;

                            Handles.color = color;
                            Handles.DrawWireDisc(node.Position, Vector3.up, radius);
                            Handles.DrawWireDisc(connectedExternal.Position, Vector3.up, radius);

                            Handles.DrawDottedLine(node.Position, connectedExternal.Position, 5f);
                        }

                        DebugLine.DrawArrow(node.Position, node.Rotation, color);
                    }
                    else
                    {
                        DebugLine.DrawArrow(node.Position, node.Rotation, Color.red);
                    }
                }
            }
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }
    }
}
