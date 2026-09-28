using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class TileGameInputBase : MonoBehaviour
    {
        public abstract bool EscapeClicked { get; }

        public abstract bool ActionClicked { get; }

        public abstract bool RotateClicked { get; }

        public abstract Vector3 GetMousePosition();
    }
}
