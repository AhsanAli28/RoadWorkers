using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileFactory : TileFactoryBase
    {
        public override RuntimeRoadTile InstantiateTile(RuntimeRoadTile prefabTile, Transform parent)
            => Instantiate(prefabTile, parent);

        public override void DestroyTile(GameObject tile)
            => Destroy(tile);
    }
}
