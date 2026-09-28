using Spirit604.Extensions;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RuntimeSegment))]
    public class RuntimeRoadTile : MonoBehaviour, IRoadTile
    {
        public TileSettings tileSettings;
        public RuntimeSegment runtimeSegment;
        public RuntimeRoadTileViewBase runtimeRoadTileView;
        public Vector2Int cellSize = new Vector2Int(1, 1);

        [Tooltip("" +
            "<b>Default</b> is used for a stub tile that transforms into any other\r\n\r\n" +
            "<b>Shape types</b> for standard shape prefabs\r\n\r\n" +
            "<b>Custom</b> for prefabs with custom shapes")]
        public PrefabType PrefabType;

        [Tooltip("The side from which the current tile is connected to other tiles & indicated by white spheres in the scene. \r\nLeft & Right - X axis, Top & Bottom - Z axis")]
        public ConnectDirection ConnectionFlags;

        [Tooltip("" +
            "<b>None</b> - a tile can't be changed under any circumstances, except by manual replacement\r\n\r\n" +
            "<b>Self</b> - a tile can change itself if the connection is not suitable\r\n\r\n" +
            "<b>Other</b> - the current tile can change neighbouring tiles if the neighbour has the 'ByNeighbour' flag & the connection is not suitable\r\n\r\n" +
            "<b>ByNeighbour</b> - a tile can be changed by a neighbour if the connection is not suitable")]
        public RecalculationType RecalculationType;

        public ConnectDirection[] ConnectionFlagArray { get; set; }

        public ConnectDirection CurrentFlags
        {
            get
            {
                var flags = ConnectDirection.None;

                for (int i = 0; i < ConnectionFlagArray?.Length; i++)
                {
                    flags |= ConnectionFlagArray[i];
                }

                return flags;
            }
        }

        public bool IsRotated => Mathf.RoundToInt(transform.rotation.eulerAngles.y) % 180 != 0;

        public Vector2Int CurrentCellSize => !IsRotated ? cellSize : new Vector2Int(cellSize.y, cellSize.x);

        public Vector2Int LeftBottom => CellUtils.GetLeftBottomCellFromObjectPosition(transform.position, CurrentCellSize, TileSize);

        public float TileSize => tileSettings.TileSize;

        public int ID { get; set; }

        public bool Selected { get; set; }

        internal int Variant { get; set; }

        public int Page { get; set; }

        public void PlaceSegment() => runtimeSegment.PlaceSegment();

        public void RemoveSegment() => runtimeSegment.RemoveSegment();

        public void SwitchAvailableState(bool available) => runtimeRoadTileView?.SwitchAvailableState(available);

        public void SwitchVisibleState(bool isVisible) => runtimeRoadTileView?.SwitchVisibleState(isVisible);

        public bool Preview
        {
            get => runtimeRoadTileView.Preview;
            internal set { if (runtimeRoadTileView) runtimeRoadTileView.Preview = value; }
        }

        public RecalculationType Type { get => RecalculationType; }

        public void SetMaterial(Material mat) => runtimeRoadTileView?.SetMaterial(mat);

        public void ResetMaterial() => runtimeRoadTileView?.ResetMaterial();

        private void Awake()
        {
            InitFlags();
        }

        public void InitFlags()
        {
            if (ConnectionFlagArray == null)
                ConnectionFlagArray = GetFlagArray();
        }

        public ConnectDirection[] GetFlagArray()
        {
            return ConnectionFlags.GetFlags().ToArray();
        }

        public void Validate()
        {
            runtimeSegment.Validate();
        }

        private void OnDrawGizmosSelected()
        {
            if (!tileSettings) return;

            var box = new Vector3(TileSize * cellSize.x, 2f, TileSize * cellSize.y);

            var matrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Gizmos.DrawWireCube(default, box);

            Gizmos.matrix = matrix;

            if (ConnectionFlags != ConnectDirection.None)
            {
                var flags = ConnectionFlags.GetFlags();

                foreach (var flag in flags)
                {
                    var offset = GetOffset(flag);

                    var pos = transform.position + transform.rotation * offset * TileSize / 2;
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
            }
        }

        private Vector3 GetOffset(ConnectDirection connectedSide)
        {
            switch (connectedSide)
            {
                case ConnectDirection.Left:
                    return new Vector3(-1, 0, 0);
                case ConnectDirection.Top:
                    return new Vector3(0, 0, 1);
                case ConnectDirection.Right:
                    return new Vector3(1, 0, 0);
                case ConnectDirection.Bottom:
                    return new Vector3(0, 0, -1);
            }

            return default;
        }

        private void Reset()
        {
            if (!runtimeSegment)
            {
                runtimeSegment = GetComponent<RuntimeSegment>();
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}
