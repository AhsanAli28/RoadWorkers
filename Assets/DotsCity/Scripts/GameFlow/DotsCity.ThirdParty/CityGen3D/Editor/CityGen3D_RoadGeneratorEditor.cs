using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    [CustomEditor(typeof(CityGen3D_RoadGenerator))]
    public class CityGen3D_RoadGeneratorEditor : SharedSettingsEditorBase<CityGen3D_RoadGeneratorEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool InternalFoldout = false;
            public bool OverlapFoldout = true;
            public bool GenerationSettingsFoldout = true;
            public bool GeneratedFoldout = true;
            public bool DebugFoldout = false;
        }

        private CityGen3D_RoadGenerator generator;

        protected override string SaveKey => "CityGen3D_RoadGeneratorEditor";

        protected override void OnEnable()
        {
            base.OnEnable();
            generator = target as CityGen3D_RoadGenerator;
            generator.UpdateSceneRoads();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Internal", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.crossingHashSize)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.edgeDistance)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.maxConnectionDistance)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.maxIntersectionDistance)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.cutOffset)));

                var splitRoadsProp = serializedObject.FindProperty(nameof(generator.splitRoads));

                EditorGUILayout.PropertyField(splitRoadsProp);

                if (splitRoadsProp.boolValue)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.generateCrossings)));

            }, ref SharedSettings.InternalFoldout);


            InspectorExtension.DrawGroupBox("Overlap", () =>
            {
                var fixOverlapCrossingsProp = serializedObject.FindProperty(nameof(generator.fixOverlapCrossings));

                EditorGUILayout.PropertyField(fixOverlapCrossingsProp);

                if (fixOverlapCrossingsProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.maxOffsetOverlap)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.additionalOverlapDistance)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.overlapDistanceRate)));
                }

            }, ref SharedSettings.OverlapFoldout);

            InspectorExtension.DrawGroupBox("Generation Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.minPathLength)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.addMinorRoads)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.addUTurns)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.cleanupOutOfTheMap)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.autoBakeRoad)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.targetRoadNames)));

            }, ref SharedSettings.GenerationSettingsFoldout);

            InspectorExtension.DrawGroupBox("Generated", () =>
            {
                DrawOptional(nameof(generator.uTurns));
                DrawOptional(nameof(generator.deadEnds));

                DrawOptional(nameof(generator.warningCreators));
                DrawOptional(nameof(generator.fixedOverlapCrossings));
                DrawOptional(nameof(generator.notFixedOverlapCrossings));
                DrawOptional(nameof(generator.partiallyFixedOverlapCrossings));
                DrawOptional(nameof(generator.ignoredRoads));

            }, ref SharedSettings.GeneratedFoldout);

            InspectorExtension.DrawGroupBox("Debug", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.showSelectButtons)));

                var drawIntersectPointsProp = serializedObject.FindProperty(nameof(generator.drawIntersectPoints));

                EditorGUILayout.PropertyField(drawIntersectPointsProp);

                if (drawIntersectPointsProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.drawIntersectAveragePoint)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.drawIntersectLocalPoints)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(generator.drawIntersectHashes)));
                }

            }, ref SharedSettings.DebugFoldout);

            if (GUILayout.Button("Generate"))
            {
                generator.Generate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOptional(string arrayPropName)
        {
            var prop = serializedObject.FindProperty(arrayPropName);

            if (prop != null && prop.arraySize > 0)
                EditorGUILayout.PropertyField(prop);
        }

        private void OnSceneGUI()
        {
            if (!generator.showSelectButtons) return;

            for (int i = 0; i < generator?.SceneRoads?.Length; i++)
            {
                var road = generator.SceneRoads[i];

                if (!road) continue;

                if (Selection.activeGameObject == road.gameObject) continue;

                var pos = road.GetCenterPoint();

                EditorExtension.DrawButton("Select", pos, 80f, () =>
                {
                    Selection.activeGameObject = road.gameObject;
                });
            }
        }

        protected override EditorSettings GetDefaultSettings() => new EditorSettings();
    }
}
