using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class ModeTypeViewElement : MonoBehaviour
    {
        [SerializeField] private PlacingType placingType;
        [SerializeField] private Button button;

        public event Action<PlacingType> OnClicked = delegate { };

        private void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClicked(placingType));
        }
    }
}
