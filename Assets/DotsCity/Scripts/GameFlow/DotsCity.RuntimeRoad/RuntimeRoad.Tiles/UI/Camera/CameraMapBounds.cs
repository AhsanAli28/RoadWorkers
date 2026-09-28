using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Limits of the movement of the `CameraFollowObject`.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraMapBounds : MonoBehaviour
    {
        public Vector2 mapSize = new Vector2(50, 50);
        public bool showBounds;

        public bool IsAvailable(Vector3 pos)
        {
            pos = transform.InverseTransformPoint(pos);
            return pos.x >= -mapSize.x / 2 && pos.x <= mapSize.x / 2 && pos.z >= -mapSize.y / 2 && pos.z <= mapSize.y / 2;
        }

        private void OnDrawGizmos()
        {
            if (!showBounds) return;

            Gizmos.DrawWireCube(transform.position, new Vector3(mapSize.x, 2, mapSize.y));
        }
    }
}
