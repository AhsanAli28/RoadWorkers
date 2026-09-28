using System;
using System.Collections.Generic;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [Serializable]
    public class PrefabData
    {
        public List<RuntimeRoadTile> Variants = new List<RuntimeRoadTile>();

        public bool HasVariants => Variants.Count > 1;
    }
}
