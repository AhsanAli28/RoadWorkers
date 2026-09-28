using Spirit604.DotsCity.Simulation.Root.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Installer
{
    /// <summary>
    /// Manually resolve reference class if Zenject is unavailable for some reason.
    /// </summary>
    public abstract class ManualReferenceInstaller : MonoBehaviour
    {
        public abstract void Resolve();

#if UNITY_EDITOR

        [ContextMenu("Rebind Inspector")]
        public void RebindInspector()
        {
            var root = GetComponentInParent<EntityRootSubsceneGenerator>();
            RebindInspector(root.gameObject);
        }

        public void RebindInspector(GameObject root)
        {
            CustomInspectorRebind(root);
            InspectorExtension.RebindInspector(this, root);
        }

        protected virtual void CustomInspectorRebind(GameObject root) { }

        protected T ResolveRefInternal<T>(GameObject root) where T : Component
        {
            return root.GetComponentInChildren<T>();
        }

        protected T[] ResolveRefsInternal<T>(GameObject root) where T : Component
        {
            return root.GetComponentsInChildren<T>();
        }
#endif
    }
}