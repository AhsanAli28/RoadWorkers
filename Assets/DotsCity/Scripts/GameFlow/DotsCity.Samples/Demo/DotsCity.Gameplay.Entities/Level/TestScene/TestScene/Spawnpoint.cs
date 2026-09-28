using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class Spawnpoint : MonoBehaviourBase
    {
        [Button]
        public void Place()
        {
            if (Physics.Raycast(transform.position + new Vector3(0, 5f), Vector3.down, out var hit, 10f, Physics.AllLayers, QueryTriggerInteraction.UseGlobal))
            {
                transform.position = hit.point;
                transform.transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            }
        }
    }
}
