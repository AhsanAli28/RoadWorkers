using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using System.Text;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeCrossSettingsDebugger : EntityDebuggerBase
    {
        private const string DebugCrosswalkDataKey = "DebugCrosswalkData";

        [System.Serializable]
        private class DebugCrosswalkData
        {
            public Color ConnectionColor = Color.yellow;
            public float LineWidth = 0.5f;
        }

        private DebugCrosswalkData debugCrosswalkData = new DebugCrosswalkData();

        private StringBuilder sb = new StringBuilder();

        public override bool HasCustomInspectorData => true;

        public PedestrianNodeCrossSettingsDebugger(EntityManager entityManager) : base(entityManager)
        {
        }

        public override Color GetBoundsColor(Entity entity)
        {
            return Color.blue;
        }

        public override void Tick(Entity entity, Color fontColor)
        {
            base.Tick(entity, fontColor);

            if (EntityManager.HasComponent<NodeLightSettingsComponent>(entity))
            {
                var nodeSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);
                var sourcePosition = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

                if (nodeSettingsComponent.HasCrosswalk && nodeSettingsComponent.CrosswalkIndex != -1)
                {
                    var connectedEntities = EntityManager.GetBuffer<NodeConnectionDataElement>(entity);

                    for (int i = 0; i < connectedEntities.Length; i++)
                    {
                        var connectedEntity = connectedEntities[i].ConnectedEntity;

                        if (connectedEntity != Entity.Null && EntityManager.HasComponent<NodeLightSettingsComponent>(connectedEntity))
                        {
                            var connectedPedestrianNodeSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(connectedEntity);

                            if (connectedPedestrianNodeSettingsComponent.CrosswalkIndex == nodeSettingsComponent.CrosswalkIndex)
                            {
                                var targetPosition = EntityManager.GetComponentData<LocalToWorld>(connectedEntity).Position;
                                DebugLine.DrawThickLine(sourcePosition, targetPosition, debugCrosswalkData.LineWidth, debugCrosswalkData.ConnectionColor);
                            }
                        }
                    }
                }
            }
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var nodeSettingsComponent = EntityManager.GetComponentData<NodeLightSettingsComponent>(entity);

            sb.Clear();
            sb.Append("CrosswalkIndex: ").Append(nodeSettingsComponent.CrosswalkIndex).Append("\n");
            sb.Append("LightIndex: ").Append(nodeSettingsComponent.LightEntity).Append("\n");

            return sb;
        }

#if UNITY_EDITOR
        public override void DrawCustomInspectorData()
        {
            EditorGUI.BeginChangeCheck();

            debugCrosswalkData.ConnectionColor = EditorGUILayout.ColorField("Crosswalk Connection Color", debugCrosswalkData.ConnectionColor);
            debugCrosswalkData.LineWidth = EditorGUILayout.FloatField("Crosswalk Connection Color", debugCrosswalkData.LineWidth);

            if (EditorGUI.EndChangeCheck())
            {
                SaveCustomData();
            }
        }
#endif

        public override object GetCustomData()
        {
            return debugCrosswalkData;
        }

        public override string GetSaveEditorDataString()
        {
            return DebugCrosswalkDataKey;
        }

        public override void LoadCustomData()
        {
            base.LoadCustomData();

            var data = LoadEditorCustomData<DebugCrosswalkData>();

            if (data != null)
            {
                debugCrosswalkData = data;
            }
        }

        public override void SaveCustomData()
        {
            SaveCustomData<DebugCrosswalkData>();
        }

        public override DebugCrosswalkData LoadEditorCustomData<DebugCrosswalkData>()
        {
            return base.LoadEditorCustomData<DebugCrosswalkData>();
        }

        public override void SaveCustomData<DebugCrosswalkData>()
        {
            base.SaveCustomData<DebugCrosswalkData>();
        }
    }
}