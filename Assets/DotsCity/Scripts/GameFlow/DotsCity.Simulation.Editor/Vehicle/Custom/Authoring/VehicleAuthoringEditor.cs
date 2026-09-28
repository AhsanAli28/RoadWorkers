#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [CustomEditor(typeof(VehicleAuthoring))]
    public class VehicleAuthoringEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficCar.html#customphysicsvehicle";

        private const string VehicleAuthoringEditorKey = "VehicleAuthoringEditor";

        private const float HandleCapOffsetValue = 0.2f;
        private const float HandleCapSize = 0.2f;
        private const float SphereCapSize = 0.1f;
        private const float DottedLineSize = 1f;

        [Serializable]
        private class VehicleAuthoringEditorSettings
        {
            public bool WheelDataFlag = true;
            public bool SuspensionFlag = true;
            public bool FrictionFlag = true;
            public bool TransientForceFlag = true;
            public bool BrakesFlag = true;
            public bool EngineFlag = true;
            public bool SceneFlag = true;
            public bool TemplateFlag = true;
            public bool WheelRefFlag = true;
            public VehicleCustomTemplate.TemplateOperationType TemplateOperationType;
            public VehicleCustomTemplate.CopySettingsType CopySettingsType = (VehicleCustomTemplate.CopySettingsType)~0;
            public VehicleCustomTemplate.TemplateNameSource NameSource;
            public VehicleCustomTemplate.TemplatePathSource PathSource;
            public string SavePath;
            public string TemplateName;
            public int TemplateIndex;
        }

        private VehicleAuthoring vehicleAuthoring;
        private static string[] templateHeaders;
        private static VehicleCustomTemplate[] templates;
        private VehicleAuthoringEditorSettings editorSettings;

        public string TemplateName { get => editorSettings.TemplateName; set => editorSettings.TemplateName = value; }

        public int TemplateIndex { get => editorSettings.TemplateIndex; set => editorSettings.TemplateIndex = value; }

        private void OnEnable()
        {
            vehicleAuthoring = target as VehicleAuthoring;
            LoadPresets();
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            InspectorExtension.DrawGroupBox("Wheel", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.WheelMass)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.MaxSteeringAngle)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.PowerSteering)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.CustomSteeringLimit)));

                if (vehicleAuthoring.CustomSteeringLimit)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.SteeringLimitCurve)));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Radius)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Width)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ApplyImpulseOffset)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.CastType)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.CastLayer)));

            }, ref editorSettings.WheelDataFlag);

            InspectorExtension.DrawGroupBox("Suspension", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.SuspensionLength)));

                if (EditorGUI.EndChangeCheck())
                {
                    var prev = vehicleAuthoring.SuspensionLength;
                    serializedObject.ApplyModifiedProperties();
                    var offset = vehicleAuthoring.SuspensionLength - prev;
                    vehicleAuthoring.MoveWheels(-offset);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Stiffness)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Damping)));

            }, ref editorSettings.SuspensionFlag);

            InspectorExtension.DrawGroupBox("Friction", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Longitudinal)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Lateral)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ForwardFriction)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.LateralFriction)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.BrakeFriction)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Drag)));

            }, ref editorSettings.FrictionFlag);

            InspectorExtension.DrawGroupBox("Transient Forces", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.UseForwardTransientForce)));

                if (vehicleAuthoring.UseForwardTransientForce)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.MinTransientForwardSpeed)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.MaxForwardFrictionRate)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ForwardRelaxMultiplier)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.UseLateralTransientForce)));

                if (vehicleAuthoring.UseLateralTransientForce)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.MinTransientLateralSpeed)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.MaxLateralFrictionRate)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.LateralRelaxMultiplier)));
                    EditorGUI.indentLevel--;
                }

            }, ref editorSettings.TransientForceFlag);

            InspectorExtension.DrawGroupBox("Brakes", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.BrakeTorque)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.HandbrakeTorque)));

            }, ref editorSettings.BrakesFlag);

            InspectorExtension.DrawGroupBox("Engine", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.Torque)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.TransmissionRate)));

            }, ref editorSettings.EngineFlag);

            InspectorExtension.DrawGroupBox("Scene Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ShowDebug)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ShowSuspensionOrigin)));

                if (vehicleAuthoring.ShowSuspensionOrigin)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.ShowSuspension)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.OriginMove)));
                }

            }, ref editorSettings.SceneFlag);

            InspectorExtension.DrawGroupBox("Template Settings", () =>
            {
                editorSettings.TemplateOperationType = InspectorExtension.DrawEnumToolbar<VehicleCustomTemplate.TemplateOperationType>(editorSettings.TemplateOperationType, true);

                switch (editorSettings.TemplateOperationType)
                {
                    case VehicleCustomTemplate.TemplateOperationType.CreateNew:
                        {
                            GUILayout.BeginVertical("HelpBox");

                            editorSettings.NameSource = (VehicleCustomTemplate.TemplateNameSource)EditorGUILayout.EnumPopup(StringExtension.CamelToLabel(nameof(editorSettings.NameSource)), editorSettings.NameSource);
                            editorSettings.PathSource = (VehicleCustomTemplate.TemplatePathSource)EditorGUILayout.EnumPopup(StringExtension.CamelToLabel(nameof(editorSettings.PathSource)), editorSettings.PathSource);

                            switch (editorSettings.NameSource)
                            {
                                case VehicleCustomTemplate.TemplateNameSource.VehicleName:
                                    {
                                        GUI.enabled = false;
                                        EditorGUILayout.TextField(StringExtension.CamelToLabel(nameof(TemplateName)), vehicleAuthoring.name);
                                        GUI.enabled = true;
                                        break;
                                    }
                                case VehicleCustomTemplate.TemplateNameSource.Custom:
                                    {
                                        TemplateName = EditorGUILayout.TextField(StringExtension.CamelToLabel(nameof(TemplateName)), TemplateName);

                                        if (string.IsNullOrEmpty(TemplateName))
                                        {
                                            TemplateName = vehicleAuthoring.name;
                                        }

                                        break;
                                    }
                            }

                            switch (editorSettings.PathSource)
                            {
                                case VehicleCustomTemplate.TemplatePathSource.Custom:
                                    {
                                        EditorGUILayout.BeginHorizontal();

                                        editorSettings.SavePath = EditorGUILayout.TextField(StringExtension.CamelToLabel(nameof(editorSettings.SavePath)), editorSettings.SavePath);

                                        if (GUILayout.Button("+", GUILayout.Width(20)))
                                        {
                                            AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select save path", ref editorSettings.SavePath, editorSettings.SavePath);
                                        }

                                        EditorGUILayout.EndHorizontal();
                                        break;
                                    }
                            }

                            if (GUILayout.Button("Create"))
                            {
                                string name = string.Empty;

                                switch (editorSettings.NameSource)
                                {
                                    case VehicleCustomTemplate.TemplateNameSource.VehicleName:
                                        {
                                            name = vehicleAuthoring.name;
                                            break;
                                        }
                                    case VehicleCustomTemplate.TemplateNameSource.Custom:
                                        {
                                            name = TemplateName;
                                            break;
                                        }
                                }

                                var path = string.Empty;

                                if (editorSettings.PathSource == VehicleCustomTemplate.TemplatePathSource.Custom)
                                {
                                    path = editorSettings.SavePath;
                                }

                                VehicleCustomTemplate.CreateTemplate(path, name, vehicleAuthoring, vehicleAuthoring.GetComponent<PhysicsBodyAuthoring>(), editorSettings.CopySettingsType);
                                LoadPresets();
                            }

                            GUILayout.EndVertical();
                            break;
                        }
                    case VehicleCustomTemplate.TemplateOperationType.CopyFromTemplate:
                        {
                            DrawTemplateSettings(editorSettings.TemplateOperationType);
                            break;
                        }
                    case VehicleCustomTemplate.TemplateOperationType.SaveToTemplate:
                        {
                            DrawTemplateSettings(editorSettings.TemplateOperationType);
                            break;
                        }
                }

            }, ref editorSettings.TemplateFlag);

            InspectorExtension.DrawGroupBox("Wheel Refs", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.SteeringWheels)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(vehicleAuthoring.AllWheels)));

            }, ref editorSettings.WheelRefFlag);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTemplateSettings(VehicleCustomTemplate.TemplateOperationType templateOperationType)
        {
            if (templateHeaders?.Length > 0)
            {
                editorSettings.CopySettingsType = InspectorExtension.DrawEnumFlagsToolbar<VehicleCustomTemplate.CopySettingsType>(editorSettings.CopySettingsType, true, "Copy Settings Type");

                GUILayout.BeginVertical("HelpBox");

                TemplateIndex = EditorGUILayout.Popup("Selected Template", TemplateIndex, templateHeaders);

                if (TemplateIndex >= templates.Length)
                {
                    TemplateIndex = 0;
                }

                var template = templates[TemplateIndex];

                EditorGUILayout.ObjectField("Template", template, typeof(VehicleCustomTemplate), false);

                string text = templateOperationType == VehicleCustomTemplate.TemplateOperationType.CopyFromTemplate ? "Copy From Template" : "Save To Template";

                if (GUILayout.Button(text))
                {
                    if (templates[TemplateIndex])
                    {
                        switch (templateOperationType)
                        {
                            case VehicleCustomTemplate.TemplateOperationType.CopyFromTemplate:
                                template.CopyFromTemplate(vehicleAuthoring, vehicleAuthoring.GetComponent<PhysicsBodyAuthoring>(), editorSettings.CopySettingsType, true);
                                break;
                            case VehicleCustomTemplate.TemplateOperationType.SaveToTemplate:
                                template.SaveToTemplate(vehicleAuthoring, vehicleAuthoring.GetComponent<PhysicsBodyAuthoring>(), editorSettings.CopySettingsType, true);
                                break;
                        }
                    }
                }

                GUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Templates not found. Make sure you have created at least one template.", MessageType.Info);
            }
        }

        private void LoadPresets()
        {
            VehicleCustomTemplateContainer.LoadPresets(out templates, out templateHeaders);
        }

        private void LoadSettings()
        {
            var json = EditorPrefs.GetString(VehicleAuthoringEditorKey);

            if (string.IsNullOrEmpty(json))
            {
                editorSettings = new VehicleAuthoringEditorSettings();
            }
            else
            {
                editorSettings = JsonUtility.FromJson<VehicleAuthoringEditorSettings>(json);
            }
        }

        private void SaveSettings()
        {
            if (editorSettings != null)
            {
                EditorPrefs.SetString(VehicleAuthoringEditorKey, JsonUtility.ToJson(editorSettings));
            }
        }

        #region Scene GUI

        private void OnSceneGUI()
        {
            if (vehicleAuthoring.ShowSuspensionOrigin)
            {
                var allWheels = vehicleAuthoring.AllWheels;

                foreach (var wheelData in allWheels)
                {
                    if (!wheelData.Wheel)
                    {
                        continue;
                    }

                    var suspensionPoint = vehicleAuthoring.GetWorldSuspensionOrigin(wheelData.Wheel.transform);
                    var wheelPoint = wheelData.Wheel.transform.position;

                    var suspensionColor = vehicleAuthoring.OriginMove == VehicleAuthoring.OriginMoveType.SuspensionOrigin || vehicleAuthoring.OriginMove == VehicleAuthoring.OriginMoveType.Suspension ? Handles.yAxisColor : Color.white;
                    var wheelColor = vehicleAuthoring.OriginMove == VehicleAuthoring.OriginMoveType.Wheel || vehicleAuthoring.OriginMove == VehicleAuthoring.OriginMoveType.Suspension ? Handles.yAxisColor : Color.white;

                    DrawSphere(suspensionPoint, suspensionColor);
                    DrawSphere(wheelPoint, wheelColor);

                    switch (vehicleAuthoring.OriginMove)
                    {
                        case VehicleAuthoring.OriginMoveType.Wheel:
                            {
                                EditorGUI.BeginChangeCheck();

                                var newPosition = DrawHandle(wheelPoint, Vector3.down);
                                var offset = newPosition - wheelPoint;

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (offset.y != 0)
                                    {
                                        offset.z = 0;
                                        offset.x = 0;
                                        vehicleAuthoring.MoveWheels(offset);
                                    }
                                }

                                break;
                            }
                        case VehicleAuthoring.OriginMoveType.SuspensionOrigin:
                            {
                                EditorGUI.BeginChangeCheck();

                                Vector3 newPosition = DrawHandle(suspensionPoint);
                                var offset = newPosition - suspensionPoint;

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (offset.y != 0)
                                    {
                                        if (offset.y != 0)
                                        {
                                            offset.z = 0;
                                            offset.x = 0;
                                            vehicleAuthoring.MoveWheels(offset);
                                        }
                                    }
                                }

                                break;
                            }
                        case VehicleAuthoring.OriginMoveType.Suspension:
                            {
                                EditorGUI.BeginChangeCheck();

                                Vector3 newPosition = DrawHandle(suspensionPoint);
                                var offset = newPosition - suspensionPoint;

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (offset.y != 0)
                                    {
                                        if (offset.y != 0)
                                        {
                                            offset.z = 0;
                                            offset.x = 0;
                                            offset = Snap(offset);

                                            vehicleAuthoring.ChangeSuspension(offset.y);
                                        }
                                    }
                                }

                                EditorGUI.BeginChangeCheck();

                                Vector3 newPosition2 = DrawHandle(wheelPoint, Vector3.down);
                                var offset2 = newPosition2 - wheelPoint;

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (offset2.y != 0)
                                    {
                                        if (offset2.y != 0)
                                        {
                                            offset2.z = 0;
                                            offset2.x = 0;
                                            offset2 = Snap(offset2);

                                            vehicleAuthoring.MoveWheels(offset2);
                                            vehicleAuthoring.ChangeSuspension(-offset2.y);
                                        }
                                    }
                                }

                                break;
                            }
                    }

                    if (vehicleAuthoring.ShowSuspension)
                    {
                        var endSuspensionPoint = vehicleAuthoring.GetWorldSuspensionEnd(wheelData.Wheel.transform);

                        var suspensionLineColor = vehicleAuthoring.OriginMove == VehicleAuthoring.OriginMoveType.Suspension ? Handles.yAxisColor : Color.white;

                        var previousColor = Handles.color;
                        Handles.color = suspensionLineColor;
                        Handles.DrawDottedLine(suspensionPoint, endSuspensionPoint, DottedLineSize);
                        Handles.color = previousColor;

                        switch (vehicleAuthoring.CastType)
                        {
                            case CastType.Ray:
                                {
                                    Handles.DrawWireDisc(wheelPoint, wheelData.Wheel.transform.right, vehicleAuthoring.Radius);
                                    break;
                                }
                            case CastType.Collider:
                                {
                                    var p1 = wheelPoint + wheelData.Wheel.transform.right * vehicleAuthoring.Width / 2;
                                    var p2 = wheelPoint - wheelData.Wheel.transform.right * vehicleAuthoring.Width / 2;

                                    Handles.DrawWireDisc(p1, wheelData.Wheel.transform.right, vehicleAuthoring.Radius);
                                    Handles.DrawWireDisc(p2, wheelData.Wheel.transform.right, vehicleAuthoring.Radius);
                                    break;
                                }
                        }
                    }
                }
            }
        }

        private Vector3 Snap(Vector3 source)
        {
            var val = source.y;
            MathUtilMethods.CustomRoundValue(ref val, 0.1f);
            return new Vector3(source.x, val, source.z);
        }

        private void DrawSphere(Vector3 point, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                var oldColor = Handles.color;
                Handles.color = color;

                Handles.SphereHandleCap(0, point, Quaternion.identity, SphereCapSize, EventType.Repaint);

                Handles.color = oldColor;
            }
        }

        private Vector3 DrawHandle(Vector3 originPoint)
        {
            return DrawHandle(originPoint, Vector3.up);
        }

        private Vector3 DrawHandle(Vector3 originPoint, Vector3 direction)
        {
            var handleOffset = direction * HandleCapOffsetValue;

            var color = Handles.color;
            Handles.color = Handles.yAxisColor;

            var newPosition = Handles.Slider(originPoint + handleOffset, direction, HandleCapSize, Handles.ConeHandleCap, 0) - handleOffset;

            Handles.color = color;

            return newPosition;
        }

        #endregion
    }
}
#endif