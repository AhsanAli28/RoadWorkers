using Spirit604.DotsCity.Gameplay.UI;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleCustomStressUI : MonoBehaviour
    {
        [SerializeField]
        private FPSDisplay fpsDisplay;

        [SerializeField]
        private Button exitButton;

        [SerializeField]
        private TextMeshProUGUI countText;

        private Coroutine routine;

        public event Action OnExitClicked = delegate { };

        private void Awake()
        {
            exitButton.onClick.AddListener(() => OnExitClicked());
        }

        private void OnDestroy()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        public void TemporalilyDisable(Func<bool> delayCallback, float delay = 0.2f)
        {
            routine = StartCoroutine(TemporalilyDisableRoutine(delayCallback, delay));
        }

        public void SwitchFPSEnabled(bool isEnabled)
        {
            if (!isEnabled)
            {
                fpsDisplay.Stop();
            }
            else
            {
                fpsDisplay.Enable();
            }
        }

        public void EnableWithDelay(Func<bool> delayCallback, float delay = 0.2f)
        {
            fpsDisplay.ResetWithDelay(delay, waitCallback: delayCallback);
        }

        public void SetCount(int count)
        {
            countText.SetText(count.ToString());
        }

        private IEnumerator TemporalilyDisableRoutine(Func<bool> delayCallback, float delay = 0.2f)
        {
            SwitchFPSEnabled(false);

            yield return new WaitForEndOfFrame();

            yield return new WaitWhile(delayCallback);

            yield return new WaitForSeconds(delay);

            SwitchFPSEnabled(true);
        }
    }
}
