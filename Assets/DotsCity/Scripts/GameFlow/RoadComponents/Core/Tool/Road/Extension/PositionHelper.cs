using UnityEngine;

namespace Spirit604.CityEditor.Utils
{
    internal static class PositionHelper
    {
        public static void RoundObjectPositionToTile(Transform transform, Vector2Int size, float tileSize)
        {
            transform.transform.position = RoundPositionToTile(transform.position, size, tileSize);
        }

        public static Vector3 RoundPositionToTile(Vector3 position, Vector2Int size, float tileSize)
        {
            var yOffset = new Vector3(0, position.y);

            var flooredPosition = GetFlooredObjectPosition(position, size, tileSize);

            position = flooredPosition;
            position += yOffset;

            return position;
        }

        public static Vector3 GetFlooredObjectPosition(Vector3 worldObjectPosition, Vector2Int size, float tileSize)
        {
            var leftBottom = GetLeftBottomCellFromObjectPosition(worldObjectPosition, size, tileSize);
            var flooredObjectPosition = GetObjectPositionFromLeftBottomCell(leftBottom, size, tileSize);

            return flooredObjectPosition;
        }

        public static Vector3 GetObjectPositionFromLeftBottomCell(Vector2Int leftBottomCorner, Vector2Int size, float tileSize)
        {
            var leftBottomPosition = TileToWorld(leftBottomCorner, tileSize);

            var offset = GetOffsetFromMapTileCenter(size, tileSize);
            var position = leftBottomPosition + offset;

            return position;
        }

        public static Vector2Int GetLeftBottomCellFromObjectPosition(Vector3 sourcePosition, Vector2Int size, float tileSize)
        {
            Vector3 offset = GetOffsetFromMapTileCenter(size, tileSize);

            Vector3 position = sourcePosition - offset;

            return WorldToTile(position, tileSize);
        }

        private static Vector3 GetOffsetFromMapTileCenter(Vector2Int size, float tileSize)
        {
            return new Vector3(tileSize * (size.x - 1) / 2, 0, tileSize * (size.y - 1) / 2);
        }

        public static Vector2Int GetObjectPositionCellFromObjectPosition(Vector3 sourcePosition, Vector2Int size, float tileSize)
        {
            var flooredPosition = GetFlooredObjectPosition(sourcePosition, size, tileSize);
            return WorldToTile(flooredPosition, tileSize);
        }

        public static Vector3 GetFlooredPosition(Vector3 worldPosition, float tileSize)
        {
            var tile = WorldToTile(worldPosition, tileSize);

            return TileToWorld(tile, tileSize);
        }

        public static Vector2Int WorldToTile(Vector3 worldPosition, float tileSize)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPosition.x / tileSize), Mathf.RoundToInt(worldPosition.z / tileSize));
        }

        public static Vector3 TileToWorld(Vector2Int tile, float tileSize)
        {
            return new Vector3(tile.x * tileSize, 0, tile.y * tileSize);
        }
    }
}
