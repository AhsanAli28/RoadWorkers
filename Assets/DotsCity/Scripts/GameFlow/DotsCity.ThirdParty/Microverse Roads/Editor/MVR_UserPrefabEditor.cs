#if UNITY_EDITOR
using JBooth.MicroVerseCore;
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.MVR
{
    [CustomEditor(typeof(MVR_UserPrefab))]
    public class MVR_UserPrefabEditor : Editor
    {
        private MVR_UserPrefab mvr_UserPrefab;
        private MVR_Config mvr_Config;
        private bool syncPrefabTransform = true;
        private Intersection currentIntersection;
        private string PrefabKeyName;
        private Intersection[] intersections;

        private void OnEnable()
        {
            mvr_UserPrefab = target as MVR_UserPrefab;
            var mvr_Generator = ObjectUtils.FindObjectOfType<MVR_Generator>();

            intersections = ObjectUtils.FindObjectsOfType<Intersection>();

            if (mvr_Generator)
                mvr_Config = mvr_Generator.Mvr_Config;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            mvr_Config = EditorGUILayout.ObjectField("MVR Config", mvr_Config, typeof(MVR_Config), false) as MVR_Config;

            var syncPrefabContent = new GUIContent()
            {
                text = "Sync Prefab Position",
                tooltip = "The local position and rotation of the created segment will be the same as the selected intersection. Make a backup of this prefab just to be safe"
            };

            syncPrefabTransform = EditorGUILayout.Toggle(syncPrefabContent, syncPrefabTransform);
            currentIntersection = EditorGUILayout.ObjectField("Selected Intersection", currentIntersection, typeof(Intersection), true) as Intersection;
            PrefabKeyName = EditorGUILayout.TextField("Prefab Key Name", PrefabKeyName);

            GUI.enabled = currentIntersection != null;

            if (GUILayout.Button("Add Prefab"))
            {
                AddPrefab();
            }

            GUI.enabled = true;
        }

        private void AddPrefab()
        {
            if (mvr_Config == null)
            {
                Debug.LogError("MVR config is null. Assign MVR config in the inspector");
                return;
            }

            var creator = mvr_UserPrefab.GetComponent<RoadSegmentCreator>();

            if (!PrefabUtility.IsAnyPrefabInstanceRoot(creator.gameObject))
            {
                Debug.LogError($"Road segment {creator.name} is not prefab. Make this road segment a prefab & try again");
                return;
            }

            if (syncPrefabTransform)
            {
                var oldPosition = currentIntersection.transform.position;
                var oldRotation = currentIntersection.transform.rotation.eulerAngles;
                creator.ChangeOffsetParentRelative(currentIntersection.transform.position, currentIntersection.transform.rotation, false);
                PrefabUtility.ApplyPrefabInstance(creator.gameObject, InteractionMode.AutomatedAction);

                Debug.Log($"Prefab position synced old position {oldPosition} oldRotation {oldRotation} new position {currentIntersection.transform.position} new rotation {currentIntersection.transform.rotation.eulerAngles}");
            }

            mvr_Config.AddPrefab(PrefabKeyName, creator);
        }

        private void OnSceneGUI()
        {
            for (int i = 0; i < intersections?.Length; i++)
            {
                var pos = intersections[i].transform.position;

                if (currentIntersection != intersections[i])
                {
                    EditorExtension.DrawButton("Select", pos, 100, () =>
                    {
                        currentIntersection = intersections[i];
                        PrefabKeyName = MVR_Generator.GetName(intersections[i].name);
                        Repaint();
                    });
                }
                else
                {
                    EditorExtension.DrawButton("Unselect", pos, 120, () =>
                    {
                        currentIntersection = null;
                        PrefabKeyName = default;
                        Repaint();
                    });
                }
            }
        }
    }
}
#endif
