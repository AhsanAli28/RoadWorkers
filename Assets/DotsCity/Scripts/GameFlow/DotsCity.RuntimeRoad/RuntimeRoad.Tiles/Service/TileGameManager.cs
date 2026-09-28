using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Main tile sample Manager, which controls all sample placement logic.
    /// </summary>
    public class TileGameManager : MonoBehaviourBase
    {
        [Tooltip("All prefabs in the factory are validated at the scene start (editor only)")]
        [SerializeField] private bool validateFactoryAtStart = true;

        [SerializeField] private TileGameUIBase tileGameUI;
        [SerializeField] private TileGameInputBase tileGameInput;
        [SerializeField] private RuntimeRoadTilePlacer runtimeRoadTilePlacer;
        [SerializeField] private TileFactoryBase tileFactoryBase;
        [SerializeField] private PathSelectorBase pedestrianPathSelector;
        [SerializeField] private PathSelectorBase trafficPathSelector;
        [SerializeField] private List<RuntimeRoadTile> initialSceneTiles = new List<RuntimeRoadTile>();

        private Dictionary<int, RuntimeRoadTile> tileBindingPrefab = new Dictionary<int, RuntimeRoadTile>();
        private Dictionary<PrefabType, RuntimeRoadTile> selectedBinding = new Dictionary<PrefabType, RuntimeRoadTile>();
        private PlacingType placingType;
        private int selectedId = -1;
        private GameModeType mode;

        private void Awake()
        {
            if (tileFactoryBase.PageCount == 0)
            {
                Debug.LogError("RuntimeRoadTilePlacer. TilePrefabDataContainer not assigned.");
            }

            tileGameUI.OnPageSelected += TileGameUI_OnPageSelected;
            tileGameUI.OnGameModeSelected += TileGameUI_OnGameModeSelected;
            tileGameUI.OnTileSelected += TileGameUI_OnTileSelected;
            tileGameUI.OnModeClicked += TileGameUI_OnModeClicked;
            tileGameUI.OnVariantSelected += TileGameUI_OnVariantSelected;
            tileGameUI.OnVariantPopupOpened += TileGameUI_OnVariantPopupOpened;
            tileGameUI.InitPages(tileFactoryBase.PageCount);
            InitFactory();
            UpdateContainer();
        }

        private void Start()
        {
            AddSceneObjects();
        }

        private void Update()
        {
            HandleCursor();
            HandleHotkey();
        }

        private void HandleCursor()
        {
            switch (placingType)
            {
                case PlacingType.Add:
                    {
                        if (runtimeRoadTilePlacer.TileSelected)
                        {
                            var worldPosition = tileGameInput.GetMousePosition();
                            runtimeRoadTilePlacer.UpdatePos(worldPosition);

                            if (tileGameInput.ActionClicked)
                            {
                                runtimeRoadTilePlacer.PlaceSegment();
                            }

                            if (tileGameInput.RotateClicked)
                            {
                                runtimeRoadTilePlacer.RotateSegment();
                            }
                        }

                        break;
                    }
                case PlacingType.Remove:
                    {
                        var worldPosition = tileGameInput.GetMousePosition();
                        runtimeRoadTilePlacer.UpdatePos(worldPosition, Vector2Int.one);

                        if (tileGameInput.ActionClicked)
                        {
                            runtimeRoadTilePlacer.RemoveSegment();
                        }

                        break;
                    }
            }
        }

        private void HandleHotkey()
        {
            if (tileGameInput.EscapeClicked)
            {
                Unselect();
            }
        }

        private void Unselect()
        {
            SetPlacingType(PlacingType.None);
            tileGameUI.Unselect();
        }

        private void AddSceneObjects()
        {
            for (int i = 0; i < initialSceneTiles.Count; i++)
            {
                var initialTile = initialSceneTiles[i];

                if (initialTile == null || !initialTile.gameObject.activeInHierarchy) continue;

                runtimeRoadTilePlacer.AddSegmentInternal(initialTile);
            }
        }

        private Vector3 GetCenterOfSceneView()
        {
            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
            return GetCenterOfSceneView(ray);
        }

        private Vector3 GetCenterOfSceneView(Ray ray)
        {
            Vector3 worldPosition = Vector3.zero;

            Plane hPlane = new Plane(Vector3.up, Vector3.zero);

            // Plane.Raycast stores the distance from ray.origin to the hit point in this variable:
            float distance = 0;

            // if the ray hits the plane...
            if (hPlane.Raycast(ray, out distance))
            {
                // get the hit point:
                worldPosition = ray.GetPoint(distance).Flat();
            }

            return worldPosition;
        }

        private void SetPlacingType(PlacingType newPlacingType)
        {
            if (placingType == newPlacingType) return;

            switch (placingType)
            {
                case PlacingType.Add:
                    runtimeRoadTilePlacer.UnselectTile();
                    break;
            }

            placingType = newPlacingType;
            runtimeRoadTilePlacer.SetPlacingType(newPlacingType);

            switch (newPlacingType)
            {
                case PlacingType.Add:
                    runtimeRoadTilePlacer.UnselectTile();
                    break;
            }
        }

        [Button]
        internal void FindInitialObjects()
        {
            initialSceneTiles = ObjectUtils.FindObjectsOfType<RuntimeRoadTile>().ToList();
            EditorSaver.SetObjectDirty(this);
        }

        private void InitFactory()
        {
            if (validateFactoryAtStart)
            {
                tileFactoryBase.Validate();
            }

            tileFactoryBase.Init();
        }

        private void UpdateContainer()
        {
            selectedBinding.Clear();
            tileBindingPrefab.Clear();

            foreach (var tile in tileFactoryBase.PrefabContainer)
            {
                for (int i = 0; i < tile.Value.Variants.Count; i++)
                {
                    RuntimeRoadTile variant = tile.Value.Variants[i];

                    var variantIndex = tileFactoryBase.GetPrefabVariant(variant.PrefabType);

                    var selected = variantIndex == i;
                    variant.Variant = i;

                    if (selected)
                    {
                        if (!selectedBinding.ContainsKey(variant.PrefabType))
                        {
                            variant.Selected = selected;
                            selectedBinding.Add(variant.PrefabType, variant);
                        }
                        else
                        {
                            if (selectedBinding[variant.PrefabType] != variant)
                            {
                                variant.Selected = false;
                            }
                        }
                    }

                    tileBindingPrefab.Add(variant.ID, variant);
                }
            }

            UpdatePrefabView();
        }

        private void UpdatePrefabView()
        {
            var prefabs = tileFactoryBase.GetPrefabs();
            tileGameUI.Populate(prefabs);
        }

        private void TileGameUI_OnPageSelected(int page)
        {
            tileFactoryBase.SelectedPage = page;
            UpdateContainer();
        }

        private void TileGameUI_OnGameModeSelected(GameModeType mode)
        {
            this.mode = mode;
            enabled = mode == GameModeType.TileMode;
            pedestrianPathSelector?.SwitchActiveState(mode == GameModeType.PedestrianPathMode);
            trafficPathSelector?.SwitchActiveState(mode == GameModeType.TrafficPathMode);

            Unselect();
            tileGameUI.SetMode(mode);
        }

        private void TileGameUI_OnTileSelected(int id)
        {
            if (runtimeRoadTilePlacer.TileSelected && selectedId == id) return;

            selectedId = id;

            if (placingType != PlacingType.Add)
            {
                SetPlacingType(PlacingType.Add);
            }

            runtimeRoadTilePlacer.SelectTile(tileBindingPrefab[id]);

            var pos = GetCenterOfSceneView();
            runtimeRoadTilePlacer.UpdatePos(pos);
        }

        private void TileGameUI_OnModeClicked(PlacingType placingType)
        {
            SetPlacingType(placingType);
        }

        private void TileGameUI_OnVariantSelected(PrefabType prefabType, int variantIndex)
        {
            var previousPrefab = selectedBinding[prefabType];
            previousPrefab.Selected = false;

            var newPrefab = tileFactoryBase.GetPrefab(prefabType, variantIndex);
            newPrefab.Selected = true;

            selectedBinding[prefabType] = newPrefab;

            tileFactoryBase.SetPrefabVariant(prefabType, variantIndex);

            tileGameUI.SetVariant(prefabType, variantIndex);
        }

        private void TileGameUI_OnVariantPopupOpened()
        {
            Unselect();
        }
    }
}
