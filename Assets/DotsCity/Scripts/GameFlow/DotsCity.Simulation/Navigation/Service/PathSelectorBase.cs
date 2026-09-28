using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public abstract class PathSelectorBase : MonoBehaviour
    {
        public enum PathCompleteActionType { WaitForNew, ContinueRandom, Custom }

        [SerializeField] private GameObject entityNodeSelectorService;
        [SerializeField] private EntityCustomPathServiceBase entityCustomPathService;
        [SerializeField] private EntityPathDrawerBase entityPathDrawerBasePrefab;
        [SerializeField] private BoundingRendererServiceBase boundingRendererService;

        [Header("Settings")]
        [Expandable]
        [SerializeField] private SelectorSettings selectorSettings;

        private IEntityNodeSelectorService entitySelectorServiceBase;
        private List<Vector3> points = new List<Vector3>();
        private EntityPathDrawerBase entityPathDrawerBase;

        public Entity SelectedEntity { get; private set; }

        public bool HasSelectedEntity => SelectedEntity != Entity.Null;

        protected abstract EntitySelectionService.EntityType EntityType { get; }

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private bool IdleOnSelect => selectorSettings.IdleOnSelect;

        private bool ShowPathPreview => selectorSettings.ShowPathPreview;

        private bool ContinueRandomOnUnselect => selectorSettings.ContinueRandomOnUnselect;

        private bool AutoStartOnFound => selectorSettings.AutoStartOnFound;

        private float EntitySelectionRadius => selectorSettings.EntitySelectionRadius;

        private PathCompleteActionType PathCompleteAction => selectorSettings.PathCompleteAction;

        protected virtual void Awake()
        {
            if (entityPathDrawerBasePrefab)
            {
                entityPathDrawerBase = Instantiate(entityPathDrawerBasePrefab, transform);
                entityPathDrawerBase.gameObject.SetActive(false);
            }

            if (entityNodeSelectorService != null && entityNodeSelectorService.TryGetComponent<IEntityNodeSelectorService>(out entitySelectorServiceBase))
            {
                entitySelectorServiceBase.Initialize();
                entitySelectorServiceBase.OnEntitySelected += EntitySelectorService_OnEntitySelected;
            }
            else
            {
                Debug.LogError($"PathSelector {name}. EntitySelectorServiceBase is null or doesn't implement <b>IEntitySceneSelectorService</b> interface");
            }

            entityCustomPathService.OnStatusUpdated += CustomPathService_OnStatusUpdated;
        }

        protected virtual void Update()
        {
            ProcessSelectedEntity();
        }

        public abstract void ChangeIdleState(Entity selectedEntity, bool isIdle);

        public void SwitchActiveState(bool isActive)
        {
            enabled = isActive;

            if (!isActive)
            {
                UnselectEntity();
            }
        }

        public void TryToSelectEntity(Vector3 worldPosition)
        {
            var newSelectedEntity = EntitySelectionService.Instance.SelectEntity(worldPosition, EntityType, EntitySelectionRadius);

            if (newSelectedEntity != Entity.Null)
            {
                SelectEntity(newSelectedEntity);
            }
        }

        public void SelectEntity(Entity newSelectedEntity)
        {
            if (SelectedEntity != Entity.Null)
            {
                var sameEntity = SelectedEntity == newSelectedEntity;

                SetColorByStatus(SelectedEntity);
                UnselectEntity();

                if (sameEntity)
                    return;
            }

            if (!CanSelect(newSelectedEntity)) return;

            SelectedEntity = newSelectedEntity;
            SwitchSelectionService(true);

            boundingRendererService?.Select(newSelectedEntity);

            if (entityCustomPathService.IsFollowingPath(newSelectedEntity))
            {
                UpdatePathView(newSelectedEntity);
            }

            if (IdleOnSelect)
            {
                ChangeIdleState(SelectedEntity, true);
            }
        }

        public void UnselectEntity(bool entityDestroyed = false)
        {
            if (SelectedEntity == Entity.Null)
                return;

            SwitchSelectionService(false);

            if (!entityDestroyed)
            {
                var status = entityCustomPathService.GetStatus(SelectedEntity);
                var missing = MissingOrCompletePath(status);

                if (EntityExist(SelectedEntity) && (!missing || !ContinueRandomOnUnselect))
                {
                    ChangeIdleState(SelectedEntity, false);
                    SetColorByStatus(SelectedEntity);
                }
                else
                {
                    RemoveEntity(SelectedEntity);
                }
            }
            else
            {
                RemoveRendererBounds(SelectedEntity);
            }

            SelectedEntity = Entity.Null;

            HidePreviewPath();
        }

        public virtual void StartPath(Entity entity)
        {
            ChangeIdleState(entity, false);
            entityCustomPathService.StartPath(entity);
        }

        protected virtual void ProcessSelectedEntity()
        {
            if (SelectedEntity == Entity.Null)
                return;

            if (!EntityExist(SelectedEntity))
            {
                UnselectEntity();
            }
        }

        protected virtual bool CanSelect(Entity entity) => EntityExist(entity);

        protected abstract bool EntityExist(Entity entity);

        protected virtual void ProcessStatusChanged(Entity entity, PathStatus pathStatus)
        {
            bool selected = EntitySelected(entity);

            switch (pathStatus)
            {
                case PathStatus.NotFound:
                    {
                        SetErrorPathView();
                        ChangeIdleState(entity, true);
                        SetColorByStatus(entity, PathStatus.NotFound);
                        break;
                    }
                case PathStatus.Found:
                    {
                        UpdatePathView(entity);

                        if (AutoStartOnFound)
                        {
                            StartPath(entity);
                        }
                        else
                        {
                            ProcessPostFound(entity);
                        }

                        SetColorByStatus(entity, PathStatus.Found, selected);
                        break;
                    }
                case PathStatus.OnTheWay:
                    {
                        SetColorByStatus(entity, PathStatus.OnTheWay, selected);
                        break;
                    }
                case PathStatus.Complete:
                    {
                        switch (PathCompleteAction)
                        {
                            case PathCompleteActionType.WaitForNew:
                                {
                                    ChangeIdleState(entity, true);
                                    SetColorByStatus(entity, PathStatus.Complete, selected);
                                    break;
                                }
                            case PathCompleteActionType.ContinueRandom:
                                {
                                    if (selected)
                                    {
                                        UnselectEntity();
                                    }
                                    else
                                    {
                                        RemoveEntity(entity);
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case PathStatus.TargetNotExist:
                    {
                        RemoveEntity(entity);

                        if (selected)
                        {
                            HidePreviewPath();
                        }

                        break;
                    }
                case PathStatus.AgentNotExist:
                    {
                        if (selected)
                        {
                            UnselectEntity(true);
                        }
                        else
                        {
                            RemoveRendererBounds(entity);
                        }

                        break;
                    }
            }
        }

        protected virtual void ProcessPostFound(Entity entity) { }

        private bool EntitySelected(Entity entity) => SelectedEntity == entity;

        private void RemoveEntity(Entity entity)
        {
            if (EntityExist(entity))
            {
                ChangeIdleState(entity, false);
                entityCustomPathService.RemoveFollowPath(entity);
            }

            RemoveRendererBounds(entity);
        }

        protected void SwitchSelectionService(bool isActive)
        {
            entitySelectorServiceBase.Draw = isActive;

            if (!isActive)
            {
                entitySelectorServiceBase.ClearAll();
            }
        }

        protected void UpdatePathView(Entity entity)
        {
            if (!entityPathDrawerBase || !ShowPathPreview || SelectedEntity != entity)
                return;

            entityCustomPathService.GetPathPoints(entity, ref points);

            entityPathDrawerBase.Initialize(points);
        }

        protected void SetErrorPathView()
        {
            if (!entityPathDrawerBase || !ShowPathPreview)
                return;

            entityPathDrawerBase.SetColor(Color.red);
        }

        protected void HidePreviewPath()
        {
            entityPathDrawerBase?.SwitchActiveState(false);
        }

        private void RemoveRendererBounds(Entity entity)
        {
            boundingRendererService?.Remove(entity);
        }

        private void EntitySelectorService_OnEntitySelected(Entity targetEntity)
        {
            if (targetEntity == Entity.Null)
                return;

            entityCustomPathService.SetFollowPath(SelectedEntity, targetEntity);
        }

        private void SetColorByStatus(Entity entity, bool selected = false)
        {
            var status = entityCustomPathService.GetStatus(entity);

            SetColorByStatus(entity, status, selected);
        }

        private void SetColorByStatus(Entity entity, PathStatus status, bool selected = false)
        {
            var color = GetColorByStatus(status, selected);

            boundingRendererService?.SetColor(entity, color);
        }

        private bool MissingOrCompletePath(PathStatus status)
        {
            return
                status == PathStatus.NotFound ||
                status == PathStatus.Complete ||
                status == PathStatus.TargetNotExist;
        }

        private Color GetColorByStatus(PathStatus status, bool selected = false)
        {
            if (selected) return Color.green;

            switch (status)
            {
                case PathStatus.WaitingForPath:
                    return Color.yellow;
                case PathStatus.TargetNotExist:
                    return Color.red;
                case PathStatus.NotFound:
                    return Color.red;
                case PathStatus.Found:
                    return Color.magenta;
                case PathStatus.OnTheWay:
                    return Color.blue;
                case PathStatus.Complete:
                    return Color.yellow;
            }

            return default;
        }

        private void CustomPathService_OnStatusUpdated(Entity entity, PathStatus pathStatus)
        {
            ProcessStatusChanged(entity, pathStatus);
        }
    }
}