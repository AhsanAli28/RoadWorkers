using Spirit604.DotsCity.Gameplay.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class TrackingPointFactory : MonoBehaviour
    {
        [SerializeField] private PlayerActor trackingPointPrefab;

        public Transform Spawn(Vector3 position, Quaternion rotation)
        {
            return Instantiate(trackingPointPrefab, position, rotation).transform;
        }
    }
}
