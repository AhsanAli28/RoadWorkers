using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class EntitySceneSelectorServiceBase<T> : SimpleFactoryBase<T>, IEntityNodeSelectorService where T : MonoBehaviour
    {
        private const string DefaultKey = "DefaultButtons";

        public class SceneEntityData
        {
            public T Button { get; set; }
            public int BindingID { get; set; }
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; }

            public Action OnClick { get; set; }
        }

        [SerializeField] private bool draw;

        [ShowIf(nameof(draw))]
        [SerializeField] private bool drawSceneMesh = true;

        [ShowIf(nameof(DrawSceneMeshFlag))]
        [SerializeField] private Mesh sceneMesh;

        [ShowIf(nameof(DrawSceneMeshFlag))]
        [SerializeField] private Material defaultMaterial;

        [ShowIf(nameof(DrawSceneMeshFlag))]
        [SerializeField] private Material selectedMaterial;

        [ShowIf(nameof(DrawSceneMeshFlag))]
        [SerializeField] private Vector3 meshOffset;

        [SerializeField] private int meshLayer = 0;

        private EntityQuery sceneEntityQuery;
        private Dictionary<int, T> buttonBinding = new Dictionary<int, T>();
        private Dictionary<string, List<SceneEntityData>> sceneButtons = new Dictionary<string, List<SceneEntityData>>();

        public Entity SelectedEntity { get; set; }

        public bool Draw
        {
            get => draw;
            set
            {
                draw = value;
                enabled = value;
            }
        }

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
        private bool DrawSceneMeshFlag => draw && drawSceneMesh;

        public event Action<Entity> OnEntitySelected = delegate { };

        protected override void Awake()
        {
            base.Awake();
            sceneEntityQuery = GetSceneEntityQuery();
        }

        private void Update()
        {
            if (!draw) return;

            ShowAll();
        }

        public void Initialize()
        {
            Populate();
        }

        public void ShowAll()
        {
            ShowAll(OnEntitySelected);
        }

        public void ShowAll(Action<Entity> onSelect)
        {
            bool hasCustom = false;

            if (sceneButtons.Count > 0)
            {
                foreach (var item in sceneButtons)
                {
                    var list = item.Value;

                    if (list.Count > 0)
                    {
                        hasCustom = true;

                        for (int i = 0; i < list.Count; i++)
                        {
                            UpdateButtonPosition(list[i].Button, list[i]);
                            DrawSceneMesh(default, list[i].Rotation, list[i].Position);
                        }
                    }
                }

                if (hasCustom)
                    return;
            }

            var poses = sceneEntityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var entities = sceneEntityQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                DrawEntity(entities[i], poses[i].Rotation, poses[i].Position, onSelect);
            }

            poses.Dispose();
            entities.Dispose();
        }

        public void SetSelectedEntities(List<SceneEntityData> entities, string key = DefaultKey)
        {
            ClearSelectedEntities(key);

            if (!sceneButtons.ContainsKey(key))
            {
                sceneButtons.Add(key, new List<SceneEntityData>());
            }

            for (int i = 0; i < entities.Count; i++)
            {
                if (!buttonBinding.ContainsKey(entities[i].BindingID))
                {
                    var button = Get();

                    entities[i].Button = button;
                    buttonBinding.Add(entities[i].BindingID, button);

                    InitButton(button, entities[i]);

                    sceneButtons[key].Add(entities[i]);
                }
            }
        }

        public void ClearSelectedEntities(string key = DefaultKey)
        {
            if (!sceneButtons.ContainsKey(key))
                return;

            var list = sceneButtons[key];

            for (int i = 0; i < list.Count; i++)
            {
                SceneEntityData data = list[i];

                if (data.Button != null)
                    data.Button.gameObject.ReturnToPool();

                if (buttonBinding.ContainsKey(data.BindingID))
                {
                    buttonBinding.Remove(data.BindingID);
                }
            }

            sceneButtons[key].Clear();
        }

        public void ClearAll()
        {
            var keys = sceneButtons.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                ClearSelectedEntities(keys[i]);
            }

            foreach (var item in buttonBinding)
            {
                item.Value.gameObject.ReturnToPool();
            }

            buttonBinding.Clear();
            SelectedEntity = default;
        }

        protected abstract EntityQuery GetSceneEntityQuery();

        protected abstract void InitWorldButton(T button, Entity entity, Vector3 pos, Action action, string text);

        protected abstract void InitButton(T button, SceneEntityData sceneEntityData);

        protected abstract void UpdateButtonPosition(T button, SceneEntityData sceneEntityData);

        protected virtual bool IsAvailable(Entity entity)
        {
            return true;
        }

        protected void RaiseOnSelect(Entity entity) => OnEntitySelected(entity);

        private void DrawEntityButton(Entity entity, Vector3 pos, Action<Entity> onSelect)
        {
            if (!IsAvailable(entity))
            {
                ReleaseButton(entity);
                return;
            }

            DrawButton(entity, pos, onSelect);
        }

        private void DrawSceneMesh(Entity entity, quaternion rot, float3 customDrawPosition)
        {
            if (!drawSceneMesh)
                return;

            var pos = (Vector3)customDrawPosition + meshOffset;

            var selected = entity == SelectedEntity;
            var material = !selected ? defaultMaterial : selectedMaterial;

            Graphics.DrawMesh(sceneMesh, pos, rot, material, meshLayer);
        }

        private T DrawButton(Entity entity, Vector3 pos, Action<Entity> onSelect)
        {
            if (!buttonBinding.ContainsKey(entity.Index))
            {
                var button = Get();

                buttonBinding.Add(entity.Index, button);

                InitWorldButton(button, entity, pos, () =>
                {
                    SelectedEntity = entity;
                    onSelect(entity);

                }, "Select");
            }

            return buttonBinding[entity.Index];
        }

        private void ReleaseButton(Entity entity)
        {
            if (buttonBinding.ContainsKey(entity.Index))
            {
                foreach (var item in sceneButtons)
                {
                    var list = item.Value;

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].BindingID == entity.Index)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }

                buttonBinding[entity.Index].gameObject.ReturnToPool();
                buttonBinding.Remove(entity.Index);
            }
        }

        private void DrawEntity(Entity entity, Quaternion rot, float3 customDrawPosition, Action<Entity> onSelect)
        {
            DrawEntityButton(entity, customDrawPosition, onSelect);
            DrawSceneMesh(entity, rot, customDrawPosition);
        }
    }
}
