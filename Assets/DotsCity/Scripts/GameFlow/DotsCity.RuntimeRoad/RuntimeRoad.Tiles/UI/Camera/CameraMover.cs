using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Controls the movement of the aim point of the `Camera`.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraMover : MonoBehaviour
    {
        private const float MaxGameViewOffset = 0.1f;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private CameraMapBounds mapBounds;
        [SerializeField] private GameObject originCameraObject;

        [SerializeField] private float maxSpeed = 40f;
        [SerializeField] private float minSpeed = 5f;
        [SerializeField] private float maxViewportOffset = 0.05f;
        [SerializeField] private bool showSceneOrigin;

        private void Update()
        {
            var mousePos = InputExtension.GetMousePosition();
            var viewPort = mainCamera.ScreenToViewportPoint(mousePos);

            if (viewPort.x < -MaxGameViewOffset || viewPort.x > 1 + MaxGameViewOffset || viewPort.y < -MaxGameViewOffset || viewPort.y > 1 + MaxGameViewOffset)
                return;

            if (viewPort.x < 0 + maxViewportOffset)
            {
                ChangeOriginPos(new Vector3(-GetSpeed(viewPort.x) * Time.deltaTime, 0));
            }
            if (viewPort.x > 1 - maxViewportOffset)
            {
                ChangeOriginPos(new Vector3(GetSpeed(viewPort.x) * Time.deltaTime, 0));
            }
            if (viewPort.y < 0 + maxViewportOffset)
            {
                ChangeOriginPos(new Vector3(0, 0, -GetSpeed(viewPort.y) * Time.deltaTime));
            }
            if (viewPort.y > 1 - maxViewportOffset)
            {
                ChangeOriginPos(new Vector3(0, 0, GetSpeed(viewPort.y) * Time.deltaTime));
            }
        }

        private void ChangeOriginPos(Vector3 offset)
        {
            var nextPos = originCameraObject.transform.position + offset;

            if (mapBounds == null || mapBounds.IsAvailable(nextPos))
            {
                originCameraObject.transform.position = nextPos;
            }
        }

        private float GetSpeed(float value)
        {
            value = Mathf.Abs(value);

            if (value > 1 - maxViewportOffset)
            {
                value = 1 - value;
            }

            var t = Mathf.Clamp01((maxViewportOffset - value) / maxViewportOffset);

            return Mathf.Lerp(minSpeed, maxSpeed, t);
        }

        private void OnDrawGizmos()
        {
            if (!showSceneOrigin) return;

            Gizmos.DrawWireSphere(originCameraObject.transform.position, 5f);
        }
    }
}
