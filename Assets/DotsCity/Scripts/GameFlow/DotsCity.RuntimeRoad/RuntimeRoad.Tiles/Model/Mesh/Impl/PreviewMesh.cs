using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class PreviewMesh : PreviewMeshBase
    {
        private const string ColorName = "_BaseColor";
        private readonly int ColorID = Shader.PropertyToID(ColorName);

        public List<MeshData> Meshes { get; set; }

        public override bool IsRendered => base.IsRendered && Meshes?.Count > 0;

        protected override int CurrentColorID => ColorID;

        public PreviewMesh(RuntimeRoadTile prefab)
        {
            var meshes = new List<MeshData>();

            var renders = prefab.runtimeRoadTileView.Renders;
            var filters = prefab.runtimeRoadTileView.Filters;

            for (int i = 0; i < renders.Count; i++)
            {
                MeshRenderer item = renders[i];

                meshes.Add(new MeshData()
                {
                    Mesh = filters[i].sharedMesh,
                });
            }

            TileSize = prefab.TileSize;
            CellSize = prefab.cellSize;
            RecalculationType = prefab.RecalculationType;
            Meshes = meshes;
            Page = prefab.Page;

            ConnectionFlagArray = prefab.GetFlagArray();
        }

        protected override void SetMaterialInternal(Material mat)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                Meshes[i].Material = mat;
            }
        }

        protected override void SetColorInternal(Color32 color)
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                Meshes[i].Material.SetColor(CurrentColorID, color);
            }
        }
    }
}
