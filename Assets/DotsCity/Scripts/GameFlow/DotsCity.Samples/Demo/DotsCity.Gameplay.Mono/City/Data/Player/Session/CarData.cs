using Spirit604.Extensions;

namespace Spirit604.Gameplay.Player.Session
{
    [System.Serializable]
    public class CarData
    {
        public bool HasData;
        public int CarModel;
        public int CurrentHealth;
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
    }
}