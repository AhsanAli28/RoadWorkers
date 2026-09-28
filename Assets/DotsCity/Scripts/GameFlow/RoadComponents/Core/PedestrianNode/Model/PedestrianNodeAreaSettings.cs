using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [System.Serializable]
    public class PedestrianNodeAreaSettings
    {
        public PedestrianAreaShapeType areaShapeType;
        public float areaSize = 5f;
        public int minSpawnCount = 2;
        public int maxSpawnCount = 5;
        public bool unlimitedTalkTime = true;
        public float minTalkTime = 20f;
        public float maxTalkTime = 50f;
        public bool showBounds = true;
        public Color boundsColor = Color.white;
    }
}