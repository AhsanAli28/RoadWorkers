using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Manager which responsible for tile layout on the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class RuntimeRoadTilePlacer : MonoBehaviourBase
    {
        private const float PreviewTileYOffset = 0.2f;

        [SerializeField] private Transform tilesParent;
        [SerializeField] private GridBoundsBase gridBoundsBase;
        [SerializeField] private GridSceneViewBase gridViewBase;
        [SerializeField] private TileGridBase tileGridBase;
        [SerializeField] private TileFactoryBase tileFactoryBase;
        [SerializeField] private PreviewServiceBase previewServiceBase;

        [Expandable]
        [SerializeField] private TileGameSettings tileGameSettings;

        private PlacingType placingType;
        private Vector2Int currentLeftBottom;
        private Vector3 currentPos;

        private RuntimeRoadTileViewBase overlappedView;
        private RuntimeRoadTile selectedTilePrefab;
        private PreviewMeshBase selectedMeshData;
        private PreviewMeshBase tempPreviewTile;
        private List<RuntimeRoadTile> existHiddenTiles = new List<RuntimeRoadTile>();
        private List<PreviewMeshBase> tempPreviewTiles = new List<PreviewMeshBase>();

        public bool TileSelected => selectedMeshData != null;

        private CollisionTileType CollisionTileType => tileGameSettings.CollisionTileType;
        private bool AutoRecalcAfterDestroy => tileGameSettings.AutoRecalcAfterDestroy;
        private bool AutoRecalcNeighbors => tileGameSettings.AutoRecalcNeighbors;
        private bool ShowPreviewTile => tileGameSettings.ShowPreviewTile;
        private bool ShowRecalculatedNeigbourPreviewTile => tileGameSettings.ShowRecalculatedNeigbourPreviewTile;

        public bool PlaceSegment()
        {
            return AddSegment();
        }

        public bool RemoveSegment()
        {
            var overlapped = tileGridBase.TryToGetFirstTile(currentPos, Vector2Int.one);

            if (overlapped != null)
            {
                RemoveSegment(overlapped.GetComponent<RuntimeRoadTile>());

                if (AutoRecalcAfterDestroy)
                {
                    for (int index = 0; index < 4; index++)
                    {
                        var directionFlag = ConnectDirection.None;

                        switch (index)
                        {
                            case 0:
                                directionFlag = ConnectDirection.Left;
                                break;
                            case 1:
                                directionFlag = ConnectDirection.Top;
                                break;
                            case 2:
                                directionFlag = ConnectDirection.Right;
                                break;
                            case 3:
                                directionFlag = ConnectDirection.Bottom;
                                break;
                        }

                        var cell = currentLeftBottom + GetDirectionOffset(directionFlag);
                        var tile = tileGridBase.TryToGetTile(cell);

                        if (tile != null)
                        {
                            var oppositeFlag = FlagUtils.GetOppositeFlag(directionFlag);
                            var roadTile = tile.GetComponent<RuntimeRoadTile>();
                            var flags = roadTile.CurrentFlags.RemoveFlag(oppositeFlag);
                            TryToRecalculateSegment(ref roadTile, flags, false);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        public bool IsAvailable(Vector3 pos, Vector2Int size)
        {
            bool isAvailable = true;

            if (CollisionTileType != CollisionTileType.Replace)
            {
                var tile = tileGridBase.TryToGetFirstTile(pos, size);

                isAvailable = tile == null;
            }

            if (isAvailable)
            {
                return gridBoundsBase.IsAvailable(selectedMeshData.Position, selectedMeshData.CurrentCellSize);
            }

            return false;
        }

        public void SetPlacingType(PlacingType newPlacingType)
        {
            placingType = newPlacingType;
            gridViewBase.SetPlacingType(newPlacingType);
        }

        public void RotateSegment()
        {
            selectedMeshData.Rotate(new Vector3(0, 90, 0));
            FlagUtils.ShiftFlags(selectedMeshData.ConnectionFlagArray);
            OnTilePosChanged();
        }

        public void UnselectTile()
        {
            if (selectedMeshData != null)
            {
                DestroyPreview(selectedMeshData);
                selectedMeshData = null;
                currentLeftBottom = default;
            }

            ResetOverllapedView();
        }

        public void UpdatePos(Vector3 pos)
        {
            UpdatePos(pos, selectedMeshData.CurrentCellSize, (newPos) =>
            {
                selectedMeshData.Position = newPos;
            });
        }

        public void UpdatePos(Vector3 pos, Vector2Int targetCellCize, Action<Vector3> onChange = null)
        {
            var roundedPos = CellUtils.GetRoundedObjectPosition(pos, targetCellCize, tileGridBase.TileSize, out var newCurrentLeftBottom);

            currentPos = roundedPos;

            if (currentLeftBottom != newCurrentLeftBottom)
            {
                onChange?.Invoke(roundedPos);

                currentLeftBottom = newCurrentLeftBottom;
                OnTilePosChanged();
            }

            gridViewBase.SetPosition(roundedPos);
        }

        public void SelectTile(RuntimeRoadTile selectedTilePrefab)
        {
            UnselectTile();
            this.selectedTilePrefab = selectedTilePrefab;
            this.selectedMeshData = previewServiceBase.CreatePreview(selectedTilePrefab);
        }

        internal void AddSegmentInternal(RuntimeRoadTile newSelectedTile, bool canRecalcNeighbor = false)
        {
            var size = newSelectedTile.CurrentCellSize;
            var pos = newSelectedTile.transform.position;
            var leftBottom = CellUtils.GetLeftBottomCellFromObjectPosition(pos, size, tileGridBase.TileSize);

            if (AutoRecalcNeighbors && newSelectedTile.ConnectionFlags != ConnectDirection.None && canRecalcNeighbor)
            {
                IterateNeighbor(ref newSelectedTile, size, leftBottom);
            }

            AddSegment(newSelectedTile);
        }

        private void IterateNeighbor(ref RuntimeRoadTile runtimeRoad, Vector2Int size, Vector2Int leftBottom, ConnectDirection connectionFlags = ConnectDirection.None, bool parent = true)
        {
            AddAllNeigbors(runtimeRoad, leftBottom, ref connectionFlags, parent);
            TryToRecalculateSegment(ref runtimeRoad, connectionFlags, parent);
        }

        private void AddAllNeigbors(IRoadTile runtimeRoad, Vector2Int leftBottom, ref ConnectDirection connectionFlags, bool parent)
        {
            for (int index = 0; index < 4; index++)
            {
                var directionFlag = ConnectDirection.None;

                switch (index)
                {
                    case 0:
                        directionFlag = ConnectDirection.Left;
                        break;
                    case 1:
                        directionFlag = ConnectDirection.Top;
                        break;
                    case 2:
                        directionFlag = ConnectDirection.Right;
                        break;
                    case 3:
                        directionFlag = ConnectDirection.Bottom;
                        break;
                }

                AddNeighborIfExist(runtimeRoad, leftBottom, directionFlag, ref connectionFlags, parent);
            }
        }

        private void TryToRecalculateSegment(ref RuntimeRoadTile runtimeRoad, ConnectDirection connectionFlags, bool parent)
        {
            if ((runtimeRoad.CurrentFlags == connectionFlags || connectionFlags == ConnectDirection.None) && runtimeRoad.PrefabType != PrefabType.Default)
                return;

            if ((!parent || !runtimeRoad.RecalculationType.HasFlag(RecalculationType.Self)) && (parent || !runtimeRoad.RecalculationType.HasFlag(RecalculationType.ByNeighbor)))
                return;

            var flagCount = FlagUtils.CountFlags((int)connectionFlags);

            GetPrefab(connectionFlags, flagCount, out var prefabType, out var prefab, out var variant);

            if (prefab == null)
                return;

            var previousPos = runtimeRoad.transform.position;
            var previousRot = runtimeRoad.transform.rotation;

            bool removed = false;

            if (prefabType != runtimeRoad.PrefabType)
            {
                removed = true;
                RemoveSegment(runtimeRoad);

                runtimeRoad = CreateTile(prefab, previousPos);
            }

            var currentPrefabFlags = runtimeRoad.ConnectionFlagArray;
            int rotatedCount = GetRotationCount(connectionFlags, currentPrefabFlags);

            if (rotatedCount > 0)
            {
                if (!removed)
                {
                    removed = true;
                    RemoveSegment(runtimeRoad);

                    runtimeRoad = CreateTile(prefab, previousPos, previousRot);
                    runtimeRoad.ConnectionFlagArray = currentPrefabFlags;
                }

                runtimeRoad.transform.rotation = runtimeRoad.transform.rotation * GetRotation(rotatedCount);
            }

            if (!removed && runtimeRoad.Variant != variant)
            {
                removed = true;
                RemoveSegment(runtimeRoad);

                runtimeRoad = CreateTile(prefab, previousPos, previousRot);
                runtimeRoad.ConnectionFlagArray = currentPrefabFlags;
            }

            if (!parent && removed)
            {
                AddSegmentInternal(runtimeRoad, false);
            }
        }

        private static Quaternion GetRotation(int rotatedCount)
        {
            return Quaternion.Euler(0, 90 * rotatedCount, 0);
        }

        private static int GetRotationCount(ConnectDirection flags, ConnectDirection[] currentPrefabFlags)
        {
            int rotatedCount = 0;

            for (int i = 0; i < 4; i++)
            {
                bool found = true;

                for (int j = 0; j < currentPrefabFlags.Length; j++)
                {
                    if (!flags.HasFlag(currentPrefabFlags[j]))
                    {
                        found = false;
                        break;
                    }
                }

                if (!found)
                {
                    rotatedCount++;

                    FlagUtils.ShiftFlags(currentPrefabFlags);
                }
            }

            return rotatedCount;
        }

        private void GetPrefab(ConnectDirection connectionFlags, int flagCount, out PrefabType prefabType, out RuntimeRoadTile prefab, out int variant, int forceVariant = -1)
        {
            prefabType = PrefabType.Default;
            prefab = null;
            variant = -1;

            switch (flagCount)
            {
                case 1:
                    {
                        prefabType = PrefabType.DeadEnd;
                        break;
                    }
                case 2:
                    {
                        if (connectionFlags.HasFlag(ConnectDirection.Left) && connectionFlags.HasFlag(ConnectDirection.Right) || connectionFlags.HasFlag(ConnectDirection.Top) && connectionFlags.HasFlag(ConnectDirection.Bottom))
                        {
                            prefabType = PrefabType.Straight;
                        }
                        else
                        {
                            prefabType = PrefabType.Turn;
                        }

                        break;
                    }
                case 3:
                    {
                        prefabType = PrefabType.TCross;
                        break;
                    }
                case 4:
                    {
                        prefabType = PrefabType.Cross;
                        break;
                    }
            }

            if (prefabType == PrefabType.Default)
                return;

            prefab = tileFactoryBase.GetPrefab(prefabType, forceVariant);

            if (prefab == null)
                return;

            variant = tileFactoryBase.GetPrefabVariant(prefabType);
        }

        private void AddNeighborIfExist(
            IRoadTile sourceTile,
            Vector2Int leftBottom,
            ConnectDirection directionFlag,
            ref ConnectDirection connectionFlags,
            bool parent)
        {
            var oppositeFlag = FlagUtils.GetOppositeFlag(directionFlag);

            if (parent && !sourceTile.Type.HasFlag(RecalculationType.Self) && !sourceTile.CurrentFlags.HasFlag(directionFlag))
                return;

            Vector2Int currentLeftBottom = leftBottom + GetDirectionOffset(directionFlag);

            var neigborTile = tileGridBase.TryToGetTile(currentLeftBottom);

            if (neigborTile == null)
                return;

            var neigborRoadTile = neigborTile.GetComponent<RuntimeRoadTile>();

            if (neigborRoadTile.Page != sourceTile.Page)
                return;

            if (!parent && !neigborRoadTile.CurrentFlags.HasFlag(oppositeFlag) && (!sourceTile.Type.HasFlag(RecalculationType.Self) && !sourceTile.CurrentFlags.HasFlag(directionFlag)))
                return;

            connectionFlags = connectionFlags | directionFlag;

            if (parent && sourceTile.Type.HasFlag(RecalculationType.Other))
                IterateNeighbor(ref neigborRoadTile, neigborRoadTile.CurrentCellSize, neigborRoadTile.LeftBottom, oppositeFlag, false);
        }

        private static Vector2Int GetDirectionOffset(ConnectDirection direction)
        {
            switch (direction)
            {
                case ConnectDirection.Left:
                    return new Vector2Int(-1, 0);
                case ConnectDirection.Top:
                    return new Vector2Int(0, 1);
                case ConnectDirection.Right:
                    return new Vector2Int(1, 0);
                case ConnectDirection.Bottom:
                    return new Vector2Int(0, -1);
            }

            return default;
        }

        private void AddSegment(RuntimeRoadTile tile)
        {
            tile.PlaceSegment();

            var size = tile.CurrentCellSize;
            var leftBottom = tile.LeftBottom;

            tileGridBase.AddSegment(tile, leftBottom, size);
        }

        private void RemoveSegment(RuntimeRoadTile tile)
        {
            tile.RemoveSegment();

            var size = tile.CurrentCellSize;
            var leftBottom = tile.LeftBottom;

            tileGridBase.RemoveSegment(tile, leftBottom, size);
            DestroyTile(tile);
        }

        private bool AddSegment()
        {
            var overlapped = tileGridBase.TryToGetFirstTile(selectedMeshData.Position, selectedMeshData.CurrentCellSize);

            if (overlapped != null)
            {
                if (CollisionTileType == CollisionTileType.Forbid)
                    return false;

                RemoveSegment(overlapped.GetComponent<RuntimeRoadTile>());
            }

            if (IsAvailable(selectedMeshData.Position, selectedMeshData.CurrentCellSize))
            {
                selectedMeshData.SwitchVisibleState(false);

                selectedMeshData.SetY(0);
                var pos = selectedMeshData.Position;
                var rot = selectedMeshData.Rotation;

                var newSelectedTile = CreateTile(selectedTilePrefab, pos, rot);

                AddSegmentInternal(newSelectedTile, true);
                return true;
            }

            return false;
        }

        private void DestroyTile(RuntimeRoadTile tile) => tileFactoryBase.DestroyTile(tile.gameObject);

        private RuntimeRoadTile CreateTile(RuntimeRoadTile prefab)
            => CreateTile(prefab, Vector3.zero, Quaternion.identity);

        private RuntimeRoadTile CreateTile(RuntimeRoadTile prefab, Vector3 pos)
            => CreateTile(prefab, pos, Quaternion.identity);

        private RuntimeRoadTile CreateTile(RuntimeRoadTile prefab, Vector3 pos, Quaternion rot)
        {
            var tile = tileFactoryBase.InstantiateTile(prefab, tilesParent);
            tile.transform.SetPositionAndRotation(pos, rot);
            tile.Variant = prefab.Variant;
            tile.ID = prefab.ID;
            tile.Page = prefab.Page;

            tile.InitFlags();
            FlagUtils.RotateTileFlags(tile.ConnectionFlagArray, tile.transform.rotation.eulerAngles.y);

            return tile;
        }

        private void ResetOverllapedView()
        {
            if (overlappedView != null)
            {
                overlappedView.SwitchVisibleState(true);
                overlappedView = null;
            }

            if (tempPreviewTile != null)
            {
                DestroyPreview(tempPreviewTile);
                tempPreviewTile = null;
            }

            if (existHiddenTiles.Count > 0)
            {
                SwitchExistHidden(true);
                existHiddenTiles.Clear();

                for (int i = 0; i < tempPreviewTiles.Count; i++)
                {
                    DestroyPreview(tempPreviewTiles[i]);
                }

                tempPreviewTiles.Clear();
            }
        }

        private void OnTilePosChanged()
        {
            if (placingType == PlacingType.Add)
            {
                selectedMeshData.SwitchVisibleState(true);

                ResetOverllapedView();

                var overlapped = tileGridBase.TryToGetFirstTile(currentPos, selectedMeshData.CurrentCellSize);

                if (ShowPreviewTile)
                {
                    var allowOverlap = CollisionTileType == CollisionTileType.Replace;

                    selectedMeshData?.SwitchAvailableState(IsAvailable(currentPos, selectedMeshData.CurrentCellSize));

                    if (overlapped != null && allowOverlap)
                    {
                        overlappedView = overlapped.GetComponent<RuntimeRoadTileViewBase>();

                        if (overlappedView != null)
                            overlappedView.SwitchVisibleState(false);
                    }

                    if (ShowRecalculatedNeigbourPreviewTile && (allowOverlap || overlapped == null))
                    {
                        var connectedFlags = ConnectDirection.None;

                        AddAllNeigbors(selectedMeshData, selectedMeshData.LeftBottom, ref connectedFlags, false);

                        if (selectedMeshData.RecalculationType.HasFlag(RecalculationType.Self))
                        {
                            var flagCount = FlagUtils.CountFlags((int)connectedFlags);

                            if (flagCount > 0)
                            {
                                selectedMeshData.SwitchVisibleState(false);

                                GetPrefab(connectedFlags, flagCount, out var prefabType, out var prefab, out var variant);

                                if (prefab != null)
                                {
                                    var array = prefab.GetFlagArray();
                                    var rotatedCount = GetRotationCount(connectedFlags, array);

                                    var position = selectedMeshData.Position;
                                    var rotation = GetRotation(rotatedCount);

                                    tempPreviewTile = previewServiceBase.CreatePreview(prefab, position, rotation);
                                }
                            }
                        }

                        var flags = connectedFlags.GetFlags().ToArray();

                        for (int i = 0; i < flags.Length; i++)
                        {
                            ConnectDirection tileFlag = flags[i];

                            var selfTile = selectedTilePrefab.RecalculationType.HasFlag(RecalculationType.Self);

                            if (!selfTile && !selectedMeshData.CurrentFlags.HasFlag(tileFlag))
                            {
                                continue;
                            }

                            var cell = selectedMeshData.LeftBottom + GetDirectionOffset(tileFlag);
                            var tile = tileGridBase.TryToGetTile(cell);
                            var roadTile = tile.GetComponent<RuntimeRoadTile>();

                            var tileConnectedFlags = ConnectDirection.None;

                            AddAllNeigbors(roadTile, roadTile.LeftBottom, ref tileConnectedFlags, false);

                            tileConnectedFlags = tileConnectedFlags | FlagUtils.GetOppositeFlag(tileFlag);

                            var currentConnectedFlagsCount = FlagUtils.CountFlags((int)tileConnectedFlags);

                            GetPrefab(tileConnectedFlags, currentConnectedFlagsCount, out var type, out var neighborPrefab, out var variant);

                            if (roadTile.PrefabType == type && (tileConnectedFlags == roadTile.CurrentFlags || selfTile))
                            {
                                continue;
                            }

                            var tempTilePreview = previewServiceBase.CreatePreview(neighborPrefab, true);

                            var rotCount = GetRotationCount(tileConnectedFlags, tempTilePreview.ConnectionFlagArray);

                            tempTilePreview.Position = tile.transform.position;
                            tempTilePreview.Rotation = GetRotation(rotCount);

                            tempPreviewTiles.Add(tempTilePreview);
                            existHiddenTiles.Add(roadTile);
                        }

                        SwitchExistHidden(false);
                    }
                }

                switch (CollisionTileType)
                {
                    case CollisionTileType.Forbid:
                        {
                            selectedMeshData.SwitchAvailableState(!overlapped);
                            selectedMeshData.SetY(0);

                            if (overlapped)
                            {
                                selectedMeshData.Position += new Vector3(0, PreviewTileYOffset);
                            }

                            break;
                        }
                }
            }

            if (placingType == PlacingType.Remove)
            {
                var overlapped = tileGridBase.TryToGetFirstTile(currentPos, Vector2Int.one);

                gridViewBase.SetRemoveColor(overlapped);
            }
        }

        private void DestroyPreview(PreviewMeshBase previewMesh)
        {
            previewServiceBase.DestroyPreview(previewMesh);
        }

        private void SwitchExistHidden(bool isActive)
        {
            for (int i = 0; i < existHiddenTiles.Count; i++)
            {
                if (existHiddenTiles[i])
                    existHiddenTiles[i].SwitchVisibleState(isActive);
            }
        }
    }
}
