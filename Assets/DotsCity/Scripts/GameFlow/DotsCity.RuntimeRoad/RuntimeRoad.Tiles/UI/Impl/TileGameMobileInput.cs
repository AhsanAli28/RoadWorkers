using Spirit604.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGameMobileInput : TileGameInputBase
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Button unselectButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private CameraMobileMover cameraMobileMover;

        private bool escapeClicked;
        private bool actionClicked;
        private bool rotateClicked;

        public override bool EscapeClicked => escapeClicked;

        public override bool ActionClicked => actionClicked;

        public override bool RotateClicked => rotateClicked;

        private void Awake()
        {
#if !UNITY_EDITOR
            enabled = false;
#endif

            unselectButton.onClick.AddListener(() =>
            {
                escapeClicked = true;
                enabled = true;
            });

            actionButton.onClick.AddListener(() =>
            {
                actionClicked = true;
                enabled = true;
            });

            rotateButton.onClick.AddListener(() =>
            {
                rotateClicked = true;
                enabled = true;
            });
        }

#if UNITY_EDITOR
        private void LateUpdate()
        {
            escapeClicked = false;
            actionClicked = false;
            rotateClicked = false;

            if (InputExtension.GetKeyDown(KeyCode.E))
            {
                actionClicked = true;
            }

            if (InputExtension.GetKeyDown(KeyCode.Escape))
            {
                escapeClicked = true;
            }

            if (InputExtension.GetKeyDown(KeyCode.CapsLock))
            {
                rotateClicked = true;
            }
        }
#else
        private void LateUpdate()
        {
            escapeClicked = false;
            actionClicked = false;
            rotateClicked = false;
            enabled = false;
        }
#endif

        public override Vector3 GetMousePosition()
        {
            var mousePosition = cameraMobileMover.PointerPos;

            var ray = mainCamera.ScreenPointToRay(mousePosition);
            return GetCenterOfSceneView(ray);
        }

        public void SwitchButtons(bool isActive)
        {
            unselectButton.gameObject.SetActive(isActive);
            actionButton.gameObject.SetActive(isActive);
            rotateButton.gameObject.SetActive(isActive);
        }

        public void SwitchAction(bool isActive)
        {
            actionButton.gameObject.SetActive(isActive);
        }

        public void SwitchUnselect(bool isActive)
        {
            unselectButton.gameObject.SetActive(isActive);
        }

        private Vector3 GetCenterOfSceneView(Ray ray)
        {
            Vector3 worldPosition = Vector3.zero;

            Plane hPlane = new Plane(Vector3.up, Vector3.zero);

            // Plane.Raycast stores the distance from ray.origin to the hit point in this variable:
            float distance = 0;

            // if the ray hits the plane...
            if (hPlane.Raycast(ray, out distance))
            {
                // get the hit point:
                worldPosition = ray.GetPoint(distance).Flat();
            }

            return worldPosition;
        }
    }
}
