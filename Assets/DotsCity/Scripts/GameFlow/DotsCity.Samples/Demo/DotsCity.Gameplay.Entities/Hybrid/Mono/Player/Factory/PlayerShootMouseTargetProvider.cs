using Spirit604.Extensions;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Player
{
    public class PlayerShootMouseTargetProvider : ShootTargetProviderBase
    {
        private readonly Camera mainCamera;
        private Vector3 worldPosition;

        public PlayerShootMouseTargetProvider(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
        }

        public override bool HasTarget => worldPosition != Vector3.zero;

        public override Vector3 GetTarget()
        {
            if (InputExtension.GetMouseButton(0))
            {
                var ray = mainCamera.ScreenPointToRay(InputExtension.GetMousePosition());

                Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                float distance = 0;

                worldPosition = Vector3.zero;

                if (hPlane.Raycast(ray, out distance))
                {
                    worldPosition = ray.GetPoint(distance).Flat();
                }

                return worldPosition;
            }

            return default;
        }

        public override bool GetShootDirection(Vector3 sourcePosition, out Vector3 shootDirection)
        {
            shootDirection = default;
            var target = GetTarget();

            if (target != default)
            {
                shootDirection = (target + TargetOffset - sourcePosition).normalized;
                return true;
            }

            return false;
        }
    }
}
