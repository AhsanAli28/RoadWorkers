using Spirit604.Extensions;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public abstract class EntityDebuggerBase : IEntityDebugger
    {
        protected EntityManager EntityManager { get; private set; }
        public virtual bool HasCustomInspectorData { get; }

        public EntityDebuggerBase(EntityManager entityManager)
        {
            this.EntityManager = entityManager;
        }

        public void DrawWireSphere(float3 position, Color color, float radius = 1f)
        {
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(position, radius);
            Gizmos.color = oldColor;
        }

        public virtual void OnSelect()
        {
            if (HasCustomInspectorData)
            {
                LoadCustomData();
            }
        }

        public virtual void OnDeselect()
        {
            if (HasCustomInspectorData)
            {
                SaveCustomData();
            }
        }

        public virtual bool HasCustomColor()
        {
            return false;
        }

        public virtual Color GetBoundsColor(Entity entity)
        {
            return Color.white;
        }

        public virtual bool ShouldDraw(Entity entity)
        {
            return true;
        }

        public virtual StringBuilder GetDescriptionText(Entity entity)
        {
            return null;
        }

        public virtual void Tick(Entity entity)
        {
        }

        public virtual void Tick(Entity entity, Color fontColor)
        {
            if (!ShouldTick(entity))
            {
                return;
            }

            Tick(entity);

            var localToWorld = EntityManager.GetComponentData<LocalToWorld>(entity);

            if (ShouldDraw(entity))
            {
                DrawDebug(entity, localToWorld.Position, localToWorld.Rotation);
            }

            StringBuilder sb = GetDescriptionText(entity);

#if UNITY_EDITOR
            if (sb != null)
            {
                EditorExtension.DrawWorldString(sb.ToString(), localToWorld.Position, fontColor);
            }
#endif
        }

        public virtual void DrawDebug(Entity entity, float3 position, quaternion rotation)
        {
            var color = GetBoundsColor(entity);
            DrawWireSphere(position, color);
        }

        public virtual object GetCustomData()
        {
            return null;
        }

        public virtual string GetSaveEditorDataString()
        {
            return string.Empty;
        }

        public virtual void SaveCustomData() { }

        public virtual void LoadCustomData() { }

        public virtual void SaveCustomData<T>() where T : class
        {
            if (!HasCustomInspectorData)
            {
                return;
            }

            var data = (T)GetCustomData();

            if (data != null)
            {
                var json = JsonUtility.ToJson(data);

#if UNITY_EDITOR
                EditorPrefs.SetString(GetSaveEditorDataString(), json);
#endif
            }
        }

        public virtual T LoadEditorCustomData<T>() where T : class
        {
            if (!HasCustomInspectorData)
            {
                return null;
            }

            T data = null;
            string json = string.Empty;

#if UNITY_EDITOR
            json = EditorPrefs.GetString(GetSaveEditorDataString());
#endif

            if (!string.IsNullOrEmpty(json))
            {
                data = JsonUtility.FromJson<T>(json);
            }

            return data;
        }

#if UNITY_EDITOR
        public virtual void DrawCustomInspectorData() { }
#endif

        protected virtual bool ShouldTick(Entity entity)
        {
            if (!EntityManager.HasComponent<LocalToWorld>(entity))
            {
                return false;
            }

            var entityPosition = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

            return (!OutOfCamera(entityPosition));
        }

        public static bool OutOfCamera(float3 position)
        {
            Camera c = Camera.current;
            if (c == null) return false;

            //// Only draw on normal cameras
            if (c.clearFlags == CameraClearFlags.Depth || c.clearFlags == CameraClearFlags.Nothing)
            {
                return true;
            }

            if (!c.InViewOfCamera(position))
            {
                return true;
            }

            return false;
        }
    }
}
