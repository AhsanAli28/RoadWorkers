#if FCG
using Spirit604.Attributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    [CreateAssetMenu(menuName = "Spirit604/FCG/FCG Prefab Container")]
    public class FCGPrefabContainer : ScriptableObject
    {
        public List<FCGPrefab> LocalBlockPrefabs = new List<FCGPrefab>();

        public bool ShowSceneBinding;

        [Layer]
        public int GroundLayer = 0;

        public string PrefabSavePath = "Assets/";

        public FCGPrefab GetPrefab(string name)
        {
            return LocalBlockPrefabs.Where(a => a.BindingName == name).FirstOrDefault();
        }
    }
}
#endif