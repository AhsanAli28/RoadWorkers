using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public interface IEntityNodeSelectorService
    {
        bool Draw { get; set; }
        Entity SelectedEntity { get; }

        event Action<Entity> OnEntitySelected;

        void Initialize();
        void ClearAll();
    }
}