using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class RampGenerator : MonoBehaviourBase
    {
        [SerializeField]
        private GameObject rampPrefab;

        [SerializeField]
        private Transform prefabParent;

        [SerializeField]
        private int xCount = 1;

        [SerializeField]
        private int zCount = 1;

        [SerializeField]
        private float xOffset = 2;

        [SerializeField]
        private float zOffset = 5;

        [Button]
        public void Generate()
        {
            while (prefabParent.childCount > 0)
            {
                DestroyImmediate(prefabParent.GetChild(0).gameObject);
            }

            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    var pos = new Vector3(x * xOffset, 0, z * zOffset);
                    var obj = GameObject.Instantiate(rampPrefab, pos, Quaternion.identity, prefabParent);
                }
            }
        }
    }
}
