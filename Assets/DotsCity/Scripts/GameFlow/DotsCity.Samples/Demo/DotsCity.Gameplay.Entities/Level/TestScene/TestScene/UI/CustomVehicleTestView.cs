using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.TestScene.UI
{
    public class CustomVehicleTestView : MonoBehaviour
    {
        [SerializeField] private Button exitButton;

        public event Action OnExitClicked = delegate { };

        private void Awake()
        {
            exitButton.onClick.AddListener(() => OnExitClicked());
        }
    }
}
