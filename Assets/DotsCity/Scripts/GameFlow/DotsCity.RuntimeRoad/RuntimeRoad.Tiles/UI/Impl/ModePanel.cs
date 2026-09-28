using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class ModePanel : MonoBehaviour
    {
        [SerializeField] private Button removeButton;

        public event Action<PlacingType> OnModeClicked = delegate { };

        private void Awake()
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => OnModeClicked(PlacingType.Remove));
        }
    }
}
