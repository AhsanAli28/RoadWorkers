using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public interface IRuntimeEntityComponentSetProvider
    {
        ComponentType[] GetComponentSet();
    }
}
