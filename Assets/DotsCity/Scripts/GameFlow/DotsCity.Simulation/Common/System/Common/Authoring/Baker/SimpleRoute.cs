using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common
{
    public class SimpleRoute : MonoBehaviour
    {
        [SerializeField] private bool showRoute;
        [SerializeField] private Color routeColor = Color.white;
        [SerializeField] private Transform[] points;

        public Transform[] Points { get => points; }

        private void OnDrawGizmos()
        {
            if (!showRoute)
            {
                return;
            }

            for (int i = 0; i < points?.Length; i++)
            {
                int nextIndex = (i + 1) % points.Length;
                Gizmos.color = routeColor;
                Gizmos.DrawLine(points[i].transform.position, points[nextIndex].transform.position);
            }
        }
    }
}
