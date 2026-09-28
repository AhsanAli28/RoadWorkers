using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(MainThreadInitGroup), OrderFirst = true)]
    public partial class InitCameraCullingSystem : SystemBase
    {
        private Camera mainCamera;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            mainCamera = Camera.main;

            if (!mainCamera)
            {
                Debug.LogError("InitCameraCullingSystem. Main camera not found. Make sure that your main camera has the 'MainCamera' tag.");
                Enabled = false;
                return;
            }

            EntityManager.CreateEntity(typeof(CameraData));
        }

        protected override void OnUpdate()
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (!mainCamera) return;

            var cameraData = new CameraData { ViewProjectionMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix };
            SystemAPI.SetSingleton(cameraData);
        }
    }
}