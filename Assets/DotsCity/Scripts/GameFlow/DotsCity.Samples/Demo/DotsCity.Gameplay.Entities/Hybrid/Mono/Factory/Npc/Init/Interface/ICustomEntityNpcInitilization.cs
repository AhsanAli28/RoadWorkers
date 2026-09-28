using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public interface ICustomEntityNpcInitilization
    {
        Entity Spawn(Transform npc);
    }
}