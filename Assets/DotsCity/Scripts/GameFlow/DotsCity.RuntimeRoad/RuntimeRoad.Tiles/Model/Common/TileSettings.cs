using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [CreateAssetMenu(menuName = "Spirit604/RuntimeDemo/Tile Settings")]
    public class TileSettings : ScriptableObject
    {
        [SerializeField] private float tileSize = 10f;

        public float TileSize { get => tileSize; set => tileSize = value; }
    }
}
