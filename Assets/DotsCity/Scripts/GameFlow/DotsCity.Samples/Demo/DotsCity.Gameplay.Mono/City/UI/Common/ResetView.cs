using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.Gameplay.UI
{
    public class ResetView : MonoBehaviour
    {
        [SerializeField] private Button resetButton;

        public event Action OnResetClicked = delegate { };

        private void Start()
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() => OnResetClicked());
        }
    }
}
