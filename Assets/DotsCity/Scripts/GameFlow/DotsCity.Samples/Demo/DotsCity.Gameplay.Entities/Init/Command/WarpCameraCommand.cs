using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Gameplay.CameraService;
using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Bootstrap
{
    public class WarpCameraCommand : BootstrapCoroutineCommandBase
    {
        private readonly CameraController cameraController;

        public WarpCameraCommand(CameraController cameraController, MonoBehaviour source) : base(source)
        {
            this.cameraController = cameraController;
        }

        protected override IEnumerator InternalRoutine()
        {
#if CINEMACHINE
            yield return cameraController.WarpCamera();
#else
            yield return null;
#endif
        }
    }
}