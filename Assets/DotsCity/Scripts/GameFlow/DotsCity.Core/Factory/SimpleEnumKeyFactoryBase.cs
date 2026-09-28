using System;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleEnumKeyFactoryBase<TEnum, TMonoObject> : SimpleTypedFactoryBase<TEnum, TMonoObject> where TEnum : Enum where TMonoObject : Component
    { }
}