using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Installer
{
    /// <summary>
    /// An analogue of the Zenject scene context that starts manual resolution for each installer.
    /// </summary>
    [DefaultExecutionOrder(-9999)]
    public class ManualSceneContext : MonoBehaviour
    {
        [SerializeField]
        private List<ManualReferenceInstaller> installers = new List<ManualReferenceInstaller>();

        [SerializeField]
        private bool autoRun = true;

        private void Awake()
        {
            if (autoRun)
            {
                StartResolve();
            }
        }

        public void StartResolve()
        {
#if !ZENJECT
            for (int i = 0; i < installers.Count; i++)
            {
                installers[i].Resolve();
            }
#endif
        }


#if UNITY_EDITOR
        [ContextMenu("Rebind Inspector")]
        public void RebindInspector()
        {
            var root = this.GetComponentInParent<EntityRootSubsceneGenerator>();
            installers = root.GetComponentsInChildren<ManualReferenceInstaller>().ToList();

            foreach (var installer in installers)
            {
                installer.RebindInspector(root.gameObject);
            }

            EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}