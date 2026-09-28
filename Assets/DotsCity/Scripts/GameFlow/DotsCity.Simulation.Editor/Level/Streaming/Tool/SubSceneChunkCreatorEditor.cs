#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [CustomEditor(typeof(SubSceneChunkCreator))]
    public class SubSceneChunkCreatorEditor : Editor
    {
        private SubSceneChunkCreator subSceneChunkCreator;

        private void OnEnable()
        {
            subSceneChunkCreator = target as SubSceneChunkCreator;
            subSceneChunkCreator.OnInspectorEnabled();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox(SubSceneChunkCreator.AssigmentsGroup, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customParent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sceneName"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("createPath"));

            }, serializedObject.FindProperty("assigmentsFlag"));

            InspectorExtension.DrawGroupBox(SubSceneChunkCreator.ChunkSettingsGroup, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("positionSourceType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("objectFindMethod"));

                if (subSceneChunkCreator.FindTargetByTag)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("targetTag"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("targetLayer"));
                }

                var disableOldSourceObjectsProp = serializedObject.FindProperty("disableOldSourceObjects");
                EditorGUILayout.PropertyField(disableOldSourceObjectsProp);

                if (disableOldSourceObjectsProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("disableSourceObjectType"));
                }

                var assignNewLayerProp = serializedObject.FindProperty("assignNewLayer");

                EditorGUILayout.PropertyField(assignNewLayerProp);

                if (assignNewLayerProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("newObjectLayer"));
                }

            }, serializedObject.FindProperty("chunkSettingsFlag"));

            InspectorExtension.DrawGroupBox(SubSceneChunkCreator.PostProcessGroup, () =>
            {
                GUILayout.BeginVertical("HelpBox");

                PhysicsShapeTransferService physicsShapeTransferService = subSceneChunkCreator.PhysicsShapeTransferService;

                if (physicsShapeTransferService)
                {
                    var copyPhysicsShapesProp = serializedObject.FindProperty("copyPhysicsShapes");

                    EditorGUILayout.PropertyField(copyPhysicsShapesProp);

                    if (copyPhysicsShapesProp.boolValue)
                    {
                        EditorGUI.indentLevel++;

                        var soTransfer = new SerializedObject(physicsShapeTransferService);
                        soTransfer.Update();

                        PhysicsShapeTransferServiceEditor.DrawSettings(physicsShapeTransferService, soTransfer);

                        soTransfer.ApplyModifiedProperties();

                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.Toggle("Copy Physics Shapes", false);
                    EditorGUILayout.HelpBox("EntityRootSubsceneGenerator not found", MessageType.Info);
                    GUI.enabled = true;
                }

                GUILayout.EndVertical();

                GUILayout.BeginVertical("HelpBox");

                var postProcessNewObjectProp = serializedObject.FindProperty("postProcessNewObject");

                EditorGUILayout.PropertyField(postProcessNewObjectProp);

                if (postProcessNewObjectProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("postProcessDatas"));
                }

                GUILayout.EndVertical();

            }, serializedObject.FindProperty("postProcessFlag"));

            InspectorExtension.DrawGroupBox(SubSceneChunkCreator.ChunkDataGroup, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chunkData"));

            }, serializedObject.FindProperty("chunkFlag"));

            InspectorExtension.DrawGroupBox(SubSceneChunkCreator.ButtonsGroup, () =>
            {
                InspectorExtension.DrawGroupBox("Scene Objects", () =>
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Enable"))
                    {
                        subSceneChunkCreator.SwitchSceneObjects(true);
                    }

                    if (GUILayout.Button("Disable"))
                    {
                        subSceneChunkCreator.SwitchSceneObjects(false);
                    }

                    GUILayout.EndHorizontal();
                });

                InspectorExtension.DrawGroupBox("SubScene Objects", () =>
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("Enable"))
                    {
                        subSceneChunkCreator.SwitchSubSceneObjects(true);
                    }

                    if (GUILayout.Button("Disable"))
                    {
                        subSceneChunkCreator.SwitchSubSceneObjects(false);
                    }

                    GUILayout.EndHorizontal();
                });

                if (GUILayout.Button("Reset Save Path"))
                {
                    subSceneChunkCreator.ResetSavePath();
                }

                if (GUILayout.Button("Clear"))
                {
                    subSceneChunkCreator.ClearButton();
                }

            }, serializedObject.FindProperty("buttonsFlag"));

            GUILayout.BeginVertical("HelpBox");

            if (GUILayout.Button("Create"))
            {
                subSceneChunkCreator.Create();
            }

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
