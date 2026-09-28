#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    [CustomEditor(typeof(EntityRootSubsceneGenerator))]
    public class EntityRootSubsceneGeneratorEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/structure.html#entity-subscene-generator";

        private EntityRootSubsceneGenerator entitySubsceneGenerator;
        private SerializedProperty physicsShapeTransferServiceProp;
        private SerializedObject physicsTransferSO;
        private PhysicsShapeTransferService physicsShapeTransferService;

        private void OnEnable()
        {
            entitySubsceneGenerator = target as EntityRootSubsceneGenerator;
            entitySubsceneGenerator.OnInspectorEnabled();
            physicsShapeTransferServiceProp = serializedObject.FindProperty("physicsShapeTransferService");
            UpdatePhysicsTransferSO();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Settings", () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(DocLink, EditorGUIUtility.singleLineHeight);

                var pedestrianNodeTransferServiceProp = serializedObject.FindProperty("pedestrianNodeTransferService");

                if (pedestrianNodeTransferServiceProp.objectReferenceValue == null)
                {
                    EditorGUILayout.PropertyField(pedestrianNodeTransferServiceProp);
                }

                if (physicsShapeTransferServiceProp.objectReferenceValue == null)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(physicsShapeTransferServiceProp);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        UpdatePhysicsTransferSO();
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("entityRoadRootPrefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("entitySubSceneSavePath"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("entitySubSceneName"));

                var citySettings = CityEditorSettings.GetSerializedSettings();

                citySettings.Update();

                var syncConfigOnChangeProp = citySettings.FindProperty("syncConfigOnChange");

                EditorGUILayout.PropertyField(syncConfigOnChangeProp);

                var r = GUILayoutUtility.GetLastRect();

                if (syncConfigOnChangeProp.boolValue)
                {
                    r.width = 25;

                    syncConfigOnChangeProp.isExpanded = EditorGUI.Foldout(r, syncConfigOnChangeProp.isExpanded, GUIContent.none);

                    if (syncConfigOnChangeProp.isExpanded)
                    {
                        EditorGUI.indentLevel++;

                        var autoOpenClosedSceneProp = citySettings.FindProperty("autoOpenClosedScene");
                        var autoCloseSceneProp = citySettings.FindProperty("autoCloseScene");

                        EditorGUILayout.PropertyField(autoOpenClosedSceneProp);

                        if (autoOpenClosedSceneProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(autoCloseSceneProp);

                            if (autoCloseSceneProp.boolValue)
                            {
                                EditorGUILayout.PropertyField(citySettings.FindProperty("autoSaveChanges"));
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }

                citySettings.ApplyModifiedProperties();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("moveTools"));

                if (entitySubsceneGenerator.DOTSSimulation)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("moveLights"));

                    var propsProperty = serializedObject.FindProperty("moveProps");

                    EditorGUILayout.PropertyField(propsProperty);

                    if (propsProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("propsSearchType"));

                        switch (entitySubsceneGenerator.PropsSearchType)
                        {
                            case EntityRootSubsceneGenerator.SearchType.ByTag:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("propsTag"));
                                break;
                            case EntityRootSubsceneGenerator.SearchType.ByLayer:
                                EditorGUILayout.PropertyField(serializedObject.FindProperty("propsLayer"));
                                break;
                        }

                        EditorGUI.indentLevel--;
                    }

                    if (entitySubsceneGenerator.MoveSurfaceAvailable)
                    {
                        var surfaceProp = serializedObject.FindProperty("moveSurface");

                        EditorGUILayout.PropertyField(surfaceProp);

                        if (surfaceProp.boolValue)
                        {
                            EditorGUI.indentLevel++;

                            EditorGUILayout.PropertyField(serializedObject.FindProperty("searchType"));

                            switch (entitySubsceneGenerator.CurrentSearchType)
                            {
                                case EntityRootSubsceneGenerator.SearchType.ByTag:
                                    EditorGUILayout.PropertyField(serializedObject.FindProperty("groundTag"));
                                    break;
                                case EntityRootSubsceneGenerator.SearchType.ByLayer:
                                    EditorGUILayout.PropertyField(serializedObject.FindProperty("groundLayer"));
                                    break;
                            }

                            EditorGUI.indentLevel--;
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUILayout.Toggle("Move Surface", false);
                        GUI.enabled = true;
                    }


                    if (physicsTransferSO != null)
                    {
                        var copyPhysicsShapesProp = serializedObject.FindProperty("copyPhysicsShapes");
                        EditorGUILayout.PropertyField(copyPhysicsShapesProp);

                        if (copyPhysicsShapesProp.boolValue)
                        {
                            physicsTransferSO.Update();

                            EditorGUI.indentLevel++;

                            PhysicsShapeTransferServiceEditor.DrawSettings(physicsShapeTransferService, physicsTransferSO);

                            EditorGUI.indentLevel--;

                            physicsTransferSO.ApplyModifiedProperties();
                        }
                    }
                }

            }, serializedObject.FindProperty("settingsFlag"));

            InspectorExtension.DrawGroupBox("Config", () =>
            {
                GUI.enabled = entitySubsceneGenerator.CanMoveBack;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Copy To Subscene"))
                {
                    entitySubsceneGenerator.CopyToSubscene();
                }

                if (GUILayout.Button("Copy From Subscene"))
                {
                    entitySubsceneGenerator.CopyFromSubscene();
                }

                EditorGUILayout.EndHorizontal();

                GUI.enabled = true;

            }, serializedObject.FindProperty("configFlag"));

            InspectorExtension.DrawGroupBox("Create Refs", () =>
            {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("entitySubScene"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("createdEntityRoadRoot"));

                serializedObject.ApplyModifiedProperties();

                var hasOperation = entitySubsceneGenerator.OperationInProgress;

                GUI.enabled = !hasOperation;

                if (GUILayout.Button("Generate"))
                {
                    entitySubsceneGenerator.Generate();
                }

                GUI.enabled = true;

                GUI.enabled = entitySubsceneGenerator.CanMoveBack;

                if (GUILayout.Button("Move Back"))
                {
                    entitySubsceneGenerator.MoveBack();
                }

                GUI.enabled = true;

            }, serializedObject.FindProperty("refsFlag"));
        }

        private void UpdatePhysicsTransferSO()
        {
            if (physicsShapeTransferServiceProp.objectReferenceValue != null)
            {
                physicsTransferSO = new SerializedObject(physicsShapeTransferServiceProp.objectReferenceValue);
                physicsShapeTransferService = physicsShapeTransferServiceProp.objectReferenceValue as PhysicsShapeTransferService;
            }
        }
    }
}
#endif