using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core.Authoring
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RuntimeEntityAuthoring), typeof(CopyTransformFromGameObjectAuthoring))]
    [RequireComponent(typeof(RuntimeEntityAuthoring))]
    public class CullPointRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#cullpoint-info")]
        [SerializeField] private string link;

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(CullPointTag) };
    }
}