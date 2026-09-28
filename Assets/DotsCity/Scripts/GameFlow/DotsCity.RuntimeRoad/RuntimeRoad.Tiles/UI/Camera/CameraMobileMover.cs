using UnityEngine;
using UnityEngine.EventSystems;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Controls the movement of the aim point of the `Camera`.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraMobileMover : MonoBehaviour, IPointerDownHandler, IPointerMoveHandler, IPointerUpHandler
    {
        private const float MaxGameViewOffset = 0.1f;

        [SerializeField] private CameraMapBounds mapBounds;
        [SerializeField] private GameObject originCameraObject;
        [SerializeField] private float sensivity = 1000f;
        [SerializeField] private bool showSceneOrigin;

        private Vector2 startPos;
        private bool started;

        public Vector2 CurrentPos { get; private set; }

        public Vector2 PointerPos { get; private set; }

        private void Update()
        {
            if (!started) return;

            var offset = new Vector3(CurrentPos.x - startPos.x, 0, CurrentPos.y - startPos.y) / sensivity;

            ChangeOriginPos(offset);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            startPos = eventData.position;
            PointerPos = startPos;
            CurrentPos = startPos;
            started = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            startPos = default;
            started = false;
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            CurrentPos = eventData.position;
        }

        private void ChangeOriginPos(Vector3 offset)
        {
            var nextPos = originCameraObject.transform.position + offset;

            if (mapBounds == null || mapBounds.IsAvailable(nextPos))
            {
                originCameraObject.transform.position = nextPos;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showSceneOrigin) return;

            Gizmos.DrawWireSphere(originCameraObject.transform.position, 5f);
        }
    }
}
