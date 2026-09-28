using Spirit604.DotsCity.Hybrid.Core;
using UnityEngine;

namespace Spirit604.Gameplay.UI
{
    public class PlayerInteractTriggerPresenter : MonoBehaviour
    {
        [SerializeField] private Transform worldCanvasParent;
        [SerializeField] private WorldInteractView worldInteractViewPrefab;

        private WorldInteractView worldInteractView;
        private bool worldButtonIsLocked;

        private void Start()
        {
            worldInteractView = Instantiate(worldInteractViewPrefab);
            worldInteractView.gameObject.SetActive(false);
            worldInteractView.transform.SetParent(worldCanvasParent);

            var viewCanvas = worldInteractView.GetComponent<Canvas>();
            viewCanvas.sortingLayerName = "UI";
            viewCanvas.sortingOrder = 1;
        }

        public void SetWorldButton(Vector3 worldPosition, System.Action onClickAction, string text = "")
        {
            worldInteractView.SetWorldButton(worldPosition, onClickAction, text);
        }

        public void SwitchWorldButtonState(bool isActive)
        {
            if (!worldButtonIsLocked)
            {
                worldInteractView.SwitchWorldButtonState(isActive);
            }
        }

        public void SwitchWorldButtonStateQuest(bool isActive)
        {
            worldButtonIsLocked = isActive;
            worldInteractView.SwitchWorldButtonState(isActive);
        }
    }
}