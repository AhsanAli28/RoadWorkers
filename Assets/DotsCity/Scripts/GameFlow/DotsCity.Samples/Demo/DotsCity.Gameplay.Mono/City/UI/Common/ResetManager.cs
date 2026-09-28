using Spirit604.Attributes;
using Spirit604.Gameplay.Services;
using UnityEngine;

namespace Spirit604.Gameplay.UI
{
    public class ResetManager : MonoBehaviour
    {
        [SerializeField] private ResetView resetView;

        private ISceneService sceneService;

        [InjectWrapper]
        public void Construct(ISceneService sceneService)
        {
            this.sceneService = sceneService;
        }

        private void Start()
        {
            resetView.OnResetClicked += ResetView_OnResetClicked;
        }

        public void DoReset()
        {
            sceneService.LoadScene(0);
        }

        private void ResetView_OnResetClicked()
        {
            DoReset();
        }
    }
}
