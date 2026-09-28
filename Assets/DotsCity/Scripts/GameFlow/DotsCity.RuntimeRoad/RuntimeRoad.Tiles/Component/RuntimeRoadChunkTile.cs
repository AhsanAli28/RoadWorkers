using Spirit604.DotsCity.Hybrid.Core;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class RuntimeRoadChunkTile : MonoBehaviour
    {
        [SerializeField] private WorldInteractView worldButton;
        [SerializeField] private RuntimeSegment runtimeSegment;
        [SerializeField] private GameObject meshParent;

        private bool isActive;

        private void Awake()
        {
            worldButton.SetWorldButton(transform.position, WorldButton_OnClick, "On");
        }

        private void SwitchActive(bool isActive)
        {
            meshParent.gameObject.SetActive(isActive);

            if (isActive)
            {
                runtimeSegment.PlaceSegment();
            }
            else
            {
                runtimeSegment.RemoveSegment();
            }
        }

        private void SwitchState()
        {
            isActive = !isActive;
            SwitchActive(isActive);

            var text = isActive ? "Off" : "On";
            worldButton.SetText(text);
        }

        private void WorldButton_OnClick()
        {
            SwitchState();
        }
    }
}
