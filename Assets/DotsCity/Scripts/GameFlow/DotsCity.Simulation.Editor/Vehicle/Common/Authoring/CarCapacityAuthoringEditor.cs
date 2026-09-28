using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
#if UNITY_EDITOR
    [CustomEditor(typeof(CarCapacityAuthoring), true)]
    public class CarCapacityAuthoringEditor : Editor
    {
        private CarCapacityAuthoring carCapacityAuthoring;

        private void OnEnable()
        {
            carCapacityAuthoring = target as CarCapacityAuthoring;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create Entry"))
            {
                carCapacityAuthoring.CreateEntry();
            }
        }

        protected virtual void OnSceneGUI()
        {
            DrawEntryHandles();
        }

        private void DrawEntryHandles()
        {
            if (!carCapacityAuthoring.ShowEntryPoint)
            {
                return;
            }

            foreach (var entryPoint in carCapacityAuthoring.EntryPoints)
            {
                if (!entryPoint)
                {
                    continue;
                }

                entryPoint.transform.position = Handles.PositionHandle(entryPoint.transform.position, Quaternion.identity);
            }
        }
    }
#endif
}