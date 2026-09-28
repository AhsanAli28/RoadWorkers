using System;

namespace Spirit604.Attributes
{
    /// <summary>
    /// Attribute wrapper for Zenject.InjectAttribute to get rid of the zenject injections if required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class InjectWrapperAttribute :
#if !ZENJECT
        Attribute
#else
        Zenject.InjectAttribute
#endif
    {
    }
}