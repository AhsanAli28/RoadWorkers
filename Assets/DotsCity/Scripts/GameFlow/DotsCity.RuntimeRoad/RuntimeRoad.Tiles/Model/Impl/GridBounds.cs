using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class GridBounds : GridBoundsBase
    {
        [SerializeField] private Vector2Int mapSize = new Vector2Int(50, 50);

        public override bool IsAvailable(Vector2Int cell)
        {
            var pos = CellUtils.GetRoundedObjectPosition(transform.position, mapSize, TileSize);
            var centerTile = CellUtils.WorldToTile(pos, TileSize);

            cell -= centerTile;

            return cell.x > -mapSize.x / 2 && cell.x < mapSize.x / 2 && cell.y > -mapSize.y / 2 && cell.y < mapSize.y / 2;
        }

        private void OnDrawGizmos()
        {
            if (!HasSettings) return;

            var pos = CellUtils.GetRoundedObjectPosition(transform.position, mapSize, TileSize);

            var size = new Vector3(TileSize * mapSize.x, 5f, TileSize * mapSize.y);
            Gizmos.DrawWireCube(pos, size);
        }
    }
}
