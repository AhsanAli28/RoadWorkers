using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class HashMapGridViewDebugger : MonoBehaviourBase
    {

#pragma warning disable 0414

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonDebug.html#hashmap-grid-debugger")]
        [SerializeField] private string link;

        [SerializeField] private bool enableDebug;

        [ShowIf(nameof(enableDebug))]
        [SerializeField] private Color lineColor = Color.white;

        [ShowIf(nameof(enableDebug))]
        [SerializeField][Range(1, 1000)] private float cellSize = HashMapHelper.DEFAULT_CELL_RADIUS;

        [ShowIf(nameof(enableDebug))]
        [SerializeField][Range(1, 1000)] private int xSize = 50;

        [ShowIf(nameof(enableDebug))]
        [SerializeField][Range(1, 1000)] private int ySize = 50;

#pragma warning restore 0414

        private void DrawCell(int x, int y)
        {
            var calcCellSize = cellSize;

            var bottomLeft = new Vector3(-x * calcCellSize, 0, -y * calcCellSize);
            var topLeft = new Vector3(-x * calcCellSize, 0, y * calcCellSize);
            var topRight = new Vector3(x * calcCellSize, 0, y * calcCellSize);
            var bottomRight = new Vector3(x * calcCellSize, 0, -y * calcCellSize);

            UnityEngine.Debug.DrawLine(bottomLeft, topLeft, lineColor);
            UnityEngine.Debug.DrawLine(topLeft, topRight, lineColor);
            UnityEngine.Debug.DrawLine(topRight, bottomRight, lineColor);
            UnityEngine.Debug.DrawLine(bottomRight, bottomLeft, lineColor);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (!enableDebug)
            {
                return;
            }

            for (int x = -xSize; x < xSize; x++)
            {
                for (int y = -ySize; y < ySize; y++)
                {
                    DrawCell(x, y);
                }
            }
        }

#endif
    }
}
