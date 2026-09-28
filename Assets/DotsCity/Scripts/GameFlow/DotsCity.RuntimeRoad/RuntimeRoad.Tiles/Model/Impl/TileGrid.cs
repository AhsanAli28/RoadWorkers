using Spirit604.Collections.Dictionary;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGrid : TileGridBase
    {
        [Serializable]
        public class RoadDictionary : AbstractSerializableDictionary<Vector2Int, RuntimeSegment> { }

        [SerializeField] private TileSettings tileSettings;
        [SerializeField] private RoadDictionary data = new RoadDictionary();

        public override float TileSize => tileSettings.TileSize;

        protected override void AddCell(RuntimeRoadTile newSelectedTile, Vector2Int cell)
        {
            data.Add(cell, newSelectedTile.runtimeSegment);
        }

        protected override void RemoveCell(Vector2Int cell)
        {
            if (data.ContainsKey(cell))
            {
                data.Remove(cell);
            }
        }

        public override RuntimeSegment TryToGetTile(Vector2Int cell)
        {
            if (data.ContainsKey(cell))
                return data[cell];

            return null;
        }
    }
}
