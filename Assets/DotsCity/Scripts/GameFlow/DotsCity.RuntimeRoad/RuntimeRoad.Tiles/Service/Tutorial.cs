using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class Tutorial : MonoBehaviour
    {
        private const string RuntimeRoadTutorialKey = "RuntimeRoadTutorial";

        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            var started = PlayerPrefs.GetInt(RuntimeRoadTutorialKey, 0);

            if (started == 0)
            {
                SwitchPanel(true);

                closeButton.onClick.AddListener(() =>
                {
                    SaveKey();
                    SwitchPanel(false);
                });
            }
        }

        private void SwitchPanel(bool isActive)
        {
            tutorialCanvas.enabled = isActive;
        }

        private void SaveKey()
        {
            PlayerPrefs.SetInt(RuntimeRoadTutorialKey, 1);
            PlayerPrefs.Save();
        }
    }
}
