using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Limit of the map of cells available for the tile prefabs.
    /// </summary>
    public abstract class GridBoundsBase : MonoBehaviour
    {
        [SerializeField] private TileSettings tileSettings;

        protected bool HasSettings => tileSettings != null;
        protected float TileSize => tileSettings.TileSize;

        public virtual bool IsAvailable(Vector3 pos, Vector2Int size)
        {
            var leftBottom = CellUtils.GetLeftBottomCellFromObjectPosition(pos, size, TileSize);

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = leftBottom + new Vector2Int(x, y);

                    var isAvailable = IsAvailable(cell);

                    if (!isAvailable)
                        return false;
                }
            }

            return true;
        }

        public abstract bool IsAvailable(Vector2Int cell);
    }
}
