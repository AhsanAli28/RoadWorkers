using System;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public interface INpcEntity : IHybridEntityRef
    {
        event Action<INpcEntity> OnDisableCallback;
    }
}