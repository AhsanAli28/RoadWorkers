using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class PreviewService : PreviewServiceBase<PreviewMesh>
    {
        [SerializeField] private int renderLayer;

        private void Update()
        {
            DrawMeshes();
        }

        protected override PreviewMesh CreatePreviewInternal(RuntimeRoadTile selectedTilePrefab, Vector3 pos, Quaternion rot)
        {
            return new PreviewMesh(selectedTilePrefab)
            {
                Position = pos,
                Rotation = rot,
            };
        }

        private void DrawMeshes()
        {
            for (int i = 0; i < previewMeshes.Count; i++)
            {
                if (!previewMeshes[i].IsRendered) continue;

                Graphics.DrawMesh(previewMeshes[i].Meshes[0].Mesh, previewMeshes[i].Position, previewMeshes[i].Rotation, previewMeshes[i].Meshes[0].Material, renderLayer);
            }
        }
    }
}
