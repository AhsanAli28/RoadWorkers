using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.Utils
{
    public class MeshRendererInfoDebug : MonoBehaviourBase
    {
        [SerializeField] private bool searchInChild = true;

        [HideIf(nameof(searchInChild))]
        [SerializeField] private MeshRenderer meshRenderer;

        [Button]
        public void ShowInfo()
        {
            MeshRenderer meshRender = null;

            if (searchInChild)
            {
                meshRender = gameObject.GetComponentInChildren<MeshRenderer>();
            }
            else
            {
                meshRender = meshRenderer;
            }

            if (meshRender)
            {
                UnityEngine.Debug.Log($"size = {meshRender.bounds.size}");
                UnityEngine.Debug.Log($"bounds = {meshRender.bounds}");
            }
            else
            {
                UnityEngine.Debug.Log("MeshRenderer not found");
            }
        }
    }
}