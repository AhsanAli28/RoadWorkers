using Spirit604.Attributes;
using Spirit604.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    [RequireComponent(typeof(LineRenderer))]
    public class EntityPathLineDrawer : EntityPathDrawerBase
    {
        [SerializeField] private LineRenderer lineRenderer;

        [OnValueChanged(nameof(ColorChanged))]
        [SerializeField] private Color color = Color.green;

        [SerializeField] private float yOffset = 1f;

        private void Awake()
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

        protected override void InitializeInternal(int count)
        {
            lineRenderer.positionCount = count;
            ColorChanged();
        }

        protected override void SetPosition(int index, float3 position)
        {
            lineRenderer.SetPosition(index, position + new float3(0, yOffset, 0));
        }

        protected override void Clear() { }

        private void ColorChanged()
        {
            SetColor(color);
        }

        private void Reset()
        {
            lineRenderer = GetComponent<LineRenderer>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}