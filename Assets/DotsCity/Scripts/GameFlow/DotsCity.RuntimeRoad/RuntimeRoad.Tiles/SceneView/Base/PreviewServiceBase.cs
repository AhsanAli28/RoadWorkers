using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Display a preview of an object on the scene before it is created.
    /// </summary>
    public abstract class PreviewServiceBase : MonoBehaviour
    {
        public abstract PreviewMeshBase CreatePreview(RuntimeRoadTile selectedTilePrefab, bool tile = false);

        public abstract PreviewMeshBase CreatePreview(RuntimeRoadTile selectedTilePrefab, Vector3 pos, Quaternion rot, bool tile = false);

        public abstract void DestroyPreview(PreviewMeshBase previewMesh);
    }

    public abstract class PreviewServiceBase<T> : PreviewServiceBase where T : PreviewMeshBase
    {
        [SerializeField] private Material previewMaterial;

        protected List<T> previewMeshes = new List<T>();

        private Material tempPreviewMaterial;
        private Material tileMaterial;

        private void Awake()
        {
            tempPreviewMaterial = Instantiate(previewMaterial);
            tileMaterial = Instantiate(previewMaterial);
        }

        public override PreviewMeshBase CreatePreview(RuntimeRoadTile selectedTilePrefab, bool tile = false)
        {
            return CreatePreview(selectedTilePrefab, Vector3.zero, Quaternion.identity, tile);
        }

        public override PreviewMeshBase CreatePreview(RuntimeRoadTile selectedTilePrefab, Vector3 pos, Quaternion rot, bool tile = false)
        {
            var selectedMeshData = CreatePreviewInternal(selectedTilePrefab, pos, rot);

            var material = !tile ? tempPreviewMaterial : tileMaterial;
            selectedMeshData.SetMaterial(material);

            previewMeshes.Add(selectedMeshData);

            return selectedMeshData;
        }

        public override void DestroyPreview(PreviewMeshBase previewMesh)
        {
            previewMeshes.Remove(previewMesh as T);
        }

        protected abstract T CreatePreviewInternal(RuntimeRoadTile selectedTilePrefab, Vector3 pos, Quaternion rot);
    }
}
