using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class PedestrianNodeIdleSettings : MonoBehaviour
    {
        public const float MIN_IDLE_TIME = 5f;
        public const float MAX_IDLE_TIME = 10F;

        [MinMaxSlider(0f, 200f)]
        [SerializeField] private Vector2 idleDuration = new Vector2(MIN_IDLE_TIME, MAX_IDLE_TIME);

        public Vector2 IdleDuration { get => idleDuration; set => idleDuration = value; }
    }
}
