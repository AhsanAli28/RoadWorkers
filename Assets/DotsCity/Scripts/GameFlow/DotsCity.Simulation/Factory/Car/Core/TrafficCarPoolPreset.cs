using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Traffic
{
    [CreateAssetMenu(fileName = "TrafficPoolPreset", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_FACTORY_PRESETS_PATH + "TrafficPoolPreset")]
    public class TrafficCarPoolPreset : ScriptableObject
    {
        [SerializeField] private EntityType trafficEntityType;
        [SerializeField] private List<CarPrefabPair> prefabDatas = new List<CarPrefabPair>();

        public EntityType TrafficEntityType { get => trafficEntityType; set => trafficEntityType = value; }

        public bool HybridPreset =>
            trafficEntityType == EntityType.HybridEntityCustomPhysics ||
            trafficEntityType == EntityType.HybridEntitySimplePhysics ||
            trafficEntityType == EntityType.HybridEntityMonoPhysics;

        public bool MonoPreset => trafficEntityType == EntityType.HybridEntityMonoPhysics;

        public List<CarPrefabPair> PrefabDatas { get => prefabDatas; set => prefabDatas = value; }

        public void AddEntry(string id, CarPrefabPair carPrefabPair, bool allowOverride = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var pair = GetPairByID(id);

            if (pair != null && !allowOverride)
            {
                return;
            }

            if (pair != null)
            {
                var index = prefabDatas.IndexOf(pair);
                prefabDatas[index] = carPrefabPair;
            }
            else
            {
                prefabDatas.Add(carPrefabPair);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public bool HasEntry(string id)
        {
            return GetPairByID(id) != null;
        }

        public void RemoveEntry(int index)
        {
            if (prefabDatas.Count > index)
            {
                prefabDatas.RemoveAt(index);
                EditorSaver.SetObjectDirty(this);
            }
        }

        public CarPrefabPair GetPairByID(string id)
        {
            return prefabDatas.Where(a => a.EntityPrefab != null && a.EntityPrefab.GetComponent<ICarIDProvider>().ID == id).FirstOrDefault();
        }

        [Button]
        public void ClearNulls(bool showMessage = true)
        {
            var index = 0;
            bool cleared = true;
            int count = 0;

            while (index < prefabDatas.Count)
            {
                if (prefabDatas[index].EntityPrefab == null)
                {
                    cleared = true;
                    prefabDatas.RemoveAt(index);
                    count++;
                }
                else
                {
                    index++;
                }
            }

            if (cleared)
            {
                EditorSaver.SetObjectDirty(this);
            }

            if (showMessage)
            {
                UnityEngine.Debug.Log($"Cleared {count} null vehicles");
            }
        }
    }
}
