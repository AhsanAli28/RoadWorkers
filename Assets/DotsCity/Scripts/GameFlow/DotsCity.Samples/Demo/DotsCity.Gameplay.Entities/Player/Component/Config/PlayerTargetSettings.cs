using Spirit604.Gameplay.Player;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public struct PlayerTargetSettings
    {
        public ShootDirectionSource PlayerShootDirectionSource;
        public float MaxTargetDistanceSQ;
        public float MaxCaptureAngle;
        public float DefaultAimPointDistance;
        public float DefaultAimPointYPosition;
    }

    public struct PlayerTargetSettingsReference : IComponentData
    {
        public BlobAssetReference<PlayerTargetSettings> Config;
    }
}