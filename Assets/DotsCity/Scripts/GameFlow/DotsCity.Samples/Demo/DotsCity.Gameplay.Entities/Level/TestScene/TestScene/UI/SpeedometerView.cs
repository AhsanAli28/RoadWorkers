using TMPro;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene.UI
{
    public class SpeedometerView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI speedText;

        public void UpdateSpeed(int speed)
        {
            speedText.SetText($"{speed} km/h");
        }
    }
}
