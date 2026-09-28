using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public class NpcCustomIdComponent : MonoBehaviour, INpcIDProvider
    {
        [SerializeField] private string id;

        public string ID => id;
    }
}
