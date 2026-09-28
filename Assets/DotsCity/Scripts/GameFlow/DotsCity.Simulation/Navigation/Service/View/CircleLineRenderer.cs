using Spirit604.Attributes;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    [RequireComponent(typeof(LineRenderer))]
    public class CircleLineRenderer : BoundingRendererBase
    {
        [SerializeField] private LineRenderer lineRenderer;

        [OnValueChanged(nameof(ColorChanged))]
        [SerializeField] private Color color = Color.green;

        [SerializeField] private float lineWidth = 0.2f;
        [SerializeField] private float yOffset = 0.2f;

        [SerializeField] private int vertexCount = 40;
        [SerializeField] private float radius = 1f;

        private void Start()
        {
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

        [Button]
        private void SetupCircle()
        {
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            float deltaTheta = (2f * Mathf.PI) / vertexCount;
            float theta = 0f;

            lineRenderer.positionCount = vertexCount;
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                Vector3 pos = new Vector3(radius * Mathf.Cos(theta), 0, radius * Mathf.Sin(theta));
                lineRenderer.SetPosition(i, pos);
                theta += deltaTheta;
            }
        }

        private void Reset()
        {
            lineRenderer = GetComponent<LineRenderer>();
            EditorSaver.SetObjectDirty(this);
        }

        private void ColorChanged()
        {
            SetColor(color);
        }
    }
}
