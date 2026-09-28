using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public static class CellUtils
    {
        public static Vector2Int GetLeftBottomCellFromObjectPosition(Vector3 sourcePosition, Vector2Int cellObjectSize, float tileSize)
        {
            Vector3 offset = GetOffsetFromMapTileCenter(cellObjectSize, tileSize);

            Vector3 position = sourcePosition - offset;

            return WorldToTile(position, tileSize);

        }

        public static Vector2Int WorldToTile(Vector3 worldPosition, float tileSize)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPosition.x / tileSize), Mathf.RoundToInt(worldPosition.z / tileSize));
        }

        public static Vector3 GetRoundedObjectPosition(Vector3 worldObjectPosition, Vector2Int cellObjectSize, float tileSize)
        {
            return GetRoundedObjectPosition(worldObjectPosition, cellObjectSize, tileSize, out var leftBottom);
        }

        public static Vector3 GetRoundedObjectPosition(Vector3 worldObjectPosition, Vector2Int cellObjectSize, float tileSize, out Vector2Int leftBottomPos)
        {
            leftBottomPos = GetLeftBottomCellFromObjectPosition(worldObjectPosition, cellObjectSize, tileSize);
            var roundedObjectPosition = GetObjectPositionFromLeftBottomCell(leftBottomPos, cellObjectSize, tileSize);

            return roundedObjectPosition;
        }

        public static Vector3 GetObjectPositionFromLeftBottomCell(Vector2Int leftBottomCorner, Vector2Int cellObjectSize, float tileSize)
        {
            var leftBottomPosition = TileToWorld(leftBottomCorner, tileSize);

            var offset = GetOffsetFromMapTileCenter(cellObjectSize, tileSize);
            var position = leftBottomPosition + offset;

            return position;
        }

        public static Vector3 TileToWorld(Vector2Int tile, float tileSize)
        {
            return new Vector3(tile.x * tileSize, 0, tile.y * tileSize);
        }

        private static Vector3 GetOffsetFromMapTileCenter(Vector2Int cellObjectSize, float tileSize)
        {
            return new Vector3(tileSize * (cellObjectSize.x - 1) / 2, 0, tileSize * (cellObjectSize.y - 1) / 2);
        }
    }
}
