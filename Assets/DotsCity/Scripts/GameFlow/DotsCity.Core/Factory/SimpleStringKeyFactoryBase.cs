using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleStringKeyFactoryBase<TMonoObject> : SimpleTypedFactoryBase<string, TMonoObject> where TMonoObject : Component
    { }
}