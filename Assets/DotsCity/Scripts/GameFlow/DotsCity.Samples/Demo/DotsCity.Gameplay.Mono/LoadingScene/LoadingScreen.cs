using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.Common
{
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [SerializeField] private Image sliderImage;
        [SerializeField] private TextMeshProUGUI progressText;

        private void Awake()
        {
            Instance = this;
        }

        public void UpdateProgress(float progress)
        {
            sliderImage.fillAmount = progress;
            progressText.text = $"{Math.Round(progress * 100, 2)}%";
        }
    }
}