using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RuntimeRoadTile))]
    public abstract class RuntimeRoadTileViewBase : MonoBehaviour
    {
        [SerializeField] protected List<MeshRenderer> renders = new List<MeshRenderer>();
        [SerializeField] protected List<MeshFilter> filters = new List<MeshFilter>();

        protected List<Material> sourceMaterials = new List<Material>();

        public IList<MeshRenderer> Renders => renders.AsReadOnly();

        public IList<MeshFilter> Filters => filters.AsReadOnly();

        public bool Preview { get; set; }

        protected virtual void Awake()
        {
            for (int i = 0; i < renders.Count; i++)
            {
                sourceMaterials.Add(renders[i].sharedMaterial);
            }
        }

        public abstract void SwitchAvailableState(bool available);

        public abstract void SwitchVisibleState(bool isVisible);

        public virtual void SetMaterial(Material material)
        {
            for (int i = 0; i < renders.Count; i++)
            {
                renders[i].sharedMaterial = material;
            }
        }

        public void ResetMaterial()
        {
            for (int i = 0; i < renders.Count; i++)
            {
                renders[i].sharedMaterial = sourceMaterials[i];
            }
        }

        private void Reset()
        {
            renders = GetComponentsInChildren<MeshRenderer>().ToList();
            filters = GetComponentsInChildren<MeshFilter>().ToList();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
