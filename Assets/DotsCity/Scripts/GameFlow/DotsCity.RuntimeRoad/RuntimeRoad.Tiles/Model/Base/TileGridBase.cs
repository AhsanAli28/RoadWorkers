using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Map of tiles added to the scene.
    /// </summary>
    public abstract class TileGridBase : MonoBehaviour
    {
        public abstract float TileSize { get; }

        public abstract RuntimeSegment TryToGetTile(Vector2Int cell);

        public virtual void AddSegment(RuntimeRoadTile newSelectedTile, Vector2Int leftBottom, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = leftBottom + new Vector2Int(x, y);

                    AddCell(newSelectedTile, cell);
                }
            }
        }

        public virtual void RemoveSegment(RuntimeRoadTile tile, Vector2Int leftBottom, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = leftBottom + new Vector2Int(x, y);

                    RemoveCell(cell);
                }
            }
        }

        public virtual RuntimeSegment TryToGetFirstTile(Vector3 pos, Vector2Int size)
        {
            var leftBottom = CellUtils.GetLeftBottomCellFromObjectPosition(pos, size, TileSize);

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cell = leftBottom + new Vector2Int(x, y);

                    var tile = TryToGetTile(cell);

                    if (tile != null)
                        return tile;
                }
            }

            return null;
        }

        protected abstract void AddCell(RuntimeRoadTile newSelectedTile, Vector2Int cell);

        protected abstract void RemoveCell(Vector2Int cell);
    }
}
