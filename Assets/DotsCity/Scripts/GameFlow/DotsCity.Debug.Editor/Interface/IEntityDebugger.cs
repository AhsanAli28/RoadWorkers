using System.Text;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public interface IEntityDebugger
    {
        void Tick(Entity entity, Color fontColor);

        bool HasCustomColor();
        Color GetBoundsColor(Entity entity);
        StringBuilder GetDescriptionText(Entity entity);
    }
}