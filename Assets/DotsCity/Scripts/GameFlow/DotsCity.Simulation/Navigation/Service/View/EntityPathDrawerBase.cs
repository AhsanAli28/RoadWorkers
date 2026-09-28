using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public abstract class EntityPathDrawerBase : MonoBehaviour
    {
        public void Initialize(List<Vector3> points)
        {
            Clear();

            InitializeInternal(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                SetPosition(i, points[i]);
            }

            SwitchActiveState(true);
        }

        public abstract void SwitchActiveState(bool isActive);

        public abstract void SetColor(Color color);

        protected abstract void InitializeInternal(int count);

        protected abstract void SetPosition(int index, float3 position);

        protected abstract void Clear();
    }
}