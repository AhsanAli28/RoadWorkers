using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class WorldUIItem : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (cam)
            {
                transform.LookAt(cam.transform);
                transform.Rotate(0, 180, 0);
            }
        }
    }
}
