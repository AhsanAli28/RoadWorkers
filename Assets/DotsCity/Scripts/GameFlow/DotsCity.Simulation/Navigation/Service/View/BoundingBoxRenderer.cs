using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    [RequireComponent(typeof(LineRenderer))]
    public class BoundingBoxRenderer : BoundingRendererBase
    {
        [SerializeField] private LineRenderer lineRenderer;

        [OnValueChanged(nameof(ColorChanged))]
        [SerializeField] private Color color = Color.green;

        [SerializeField] private float lineWidth = 0.2f;
        [SerializeField] private float boundsOffset = 0.2f;
        [SerializeField] private float yOffset = 0.2f;

        private void Start()
        {
            lineRenderer.positionCount = 4;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            ColorChanged();
        }

        public override void SwitchActiveState(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public override void SetColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        public override void ResetColor()
        {
            SetColor(color);
        }

        protected override void UpdatePosInternal(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos + new Vector3(0, yOffset), rot);
        }

        protected override void InitInternal(Entity entity)
        {
            base.InitInternal(entity);

            var bounds = EntityManager.GetComponentData<BoundsComponent>(Entity).Bounds;

            lineRenderer.SetPosition(0, new Vector3(bounds.min.x - boundsOffset, bounds.min.y, bounds.max.z + boundsOffset));
            lineRenderer.SetPosition(1, new Vector3(bounds.max.x + boundsOffset, bounds.min.y, bounds.max.z + boundsOffset));

            lineRenderer.SetPosition(2, new Vector3(bounds.max.x + boundsOffset, bounds.min.y, bounds.min.z - boundsOffset));
            lineRenderer.SetPosition(3, new Vector3(bounds.min.x - boundsOffset, bounds.min.y, bounds.min.z - boundsOffset));
        }

        private void Reset()
        {
            lineRenderer = GetComponent<LineRenderer>();
            EditorSaver.SetObjectDirty(this);
        }

        private void ColorChanged()
        {
            ResetColor();
        }
    }
}
