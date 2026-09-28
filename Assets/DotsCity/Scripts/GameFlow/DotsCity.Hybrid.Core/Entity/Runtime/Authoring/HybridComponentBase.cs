using Spirit604.CityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public abstract class HybridComponentBase : ScriptableObject
    {
        public const string BasePath = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Entities/";

        public List<HybridComponentBase> RequiredComponents = new List<HybridComponentBase>();
        public List<HybridComponentBase> UsedBy = new List<HybridComponentBase>();
    }
}
