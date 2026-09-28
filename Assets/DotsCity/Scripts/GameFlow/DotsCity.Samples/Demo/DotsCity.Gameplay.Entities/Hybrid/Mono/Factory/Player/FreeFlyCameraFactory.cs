using Spirit604.DotsCity.Gameplay.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class FreeFlyCameraFactory : MonoBehaviour
    {
        [SerializeField] private PlayerActor freeFlyCameraPrefab;

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            return Instantiate(freeFlyCameraPrefab, position, rotation).gameObject;
        }
    }
}
