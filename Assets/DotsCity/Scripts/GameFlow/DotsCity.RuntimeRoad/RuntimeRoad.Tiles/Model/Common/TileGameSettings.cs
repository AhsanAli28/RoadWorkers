using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [CreateAssetMenu(menuName = "Spirit604/RuntimeDemo/Tile Game Settings")]
    public class TileGameSettings : ScriptableObjectBase
    {
        [SerializeField] private CollisionTileType collisionTileType;
        [SerializeField] private bool autoRecalcNeighbors = true;
        [SerializeField] private bool autoRecalcAfterDestroy = true;

        [Tooltip("Current tiles will be previewed when a suitable replacement tile is found")]
        [SerializeField] private bool showRecalculatedPreviewTile = true;

        [ShowIf(nameof(showRecalculatedPreviewTile))]
        [Tooltip("Neighboring tiles are previewed when a suitable replacement tile is found")]
        [SerializeField] private bool showRecalculatedNeigbourPreviewTile = true;

        public CollisionTileType CollisionTileType { get => collisionTileType; set => collisionTileType = value; }
        public bool AutoRecalcNeighbors { get => autoRecalcNeighbors; set => autoRecalcNeighbors = value; }
        public bool AutoRecalcAfterDestroy { get => autoRecalcAfterDestroy; set => autoRecalcAfterDestroy = value; }
        public bool ShowPreviewTile { get => showRecalculatedPreviewTile; set => showRecalculatedPreviewTile = value; }
        public bool ShowRecalculatedNeigbourPreviewTile { get => showRecalculatedNeigbourPreviewTile; set => showRecalculatedNeigbourPreviewTile = value; }
    }
}
