using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public struct PlayerCarCollectionBlob
    {
        public BlobArray<int> AvailableIds;
    }

    public struct PlayerCarCollectionReference : IComponentData
    {
        public BlobAssetReference<PlayerCarCollectionBlob> Config;
    }
}