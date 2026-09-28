using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.VFX;
using Spirit604.DotsCity.Simulation.VFX;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class BulletHitReactionUtils
    {
        private static readonly Vector3 yHitOffset = new Vector3(0, 1f);

        public static void CreateBulletVfx(VFXFactory vFXFactory, HealthComponent healthComponent)
        {
            CreateBulletVfx(vFXFactory, healthComponent.HitPosition, healthComponent.HitDirection);
        }

        public static void CreateBulletVfx(VFXFactory vFXFactory, Vector3 hitPosition, Vector3 hitDirection)
        {
            if (vFXFactory == null)
                return;

            var vfx = vFXFactory.GetVFX(VFXType.DefaultBulletSparks);

            if (vfx)
            {
                hitPosition += yHitOffset;

                Quaternion rotation = Quaternion.LookRotation(-hitDirection);

                vfx.transform.rotation = rotation;
                vfx.GetComponent<VFXBehaviour>().PlayOneShot(hitPosition);
            }
        }
    }
}