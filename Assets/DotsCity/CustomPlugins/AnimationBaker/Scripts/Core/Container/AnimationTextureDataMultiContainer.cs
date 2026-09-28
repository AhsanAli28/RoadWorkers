using Spirit604.AnimationBaker.EditorInternal;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class AnimationTextureDataMultiContainer : AnimationTextureDataContainerBase
    {
        [SerializeField] private List<AnimationTextureDataContainer> containers = new List<AnimationTextureDataContainer>();

        public override ContainerType CurrentContainerType => ContainerType.Multi;

        public List<AnimationTextureDataContainer> Containers { get => containers; set => containers = value; }

        internal void AddContainer(AnimationTextureDataContainer animationSheetData)
        {
            containers.Add(animationSheetData);
            EditorSaver.SetObjectDirty(this);
        }
    }
}