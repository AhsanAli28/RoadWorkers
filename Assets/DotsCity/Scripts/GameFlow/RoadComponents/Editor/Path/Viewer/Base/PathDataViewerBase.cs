#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public abstract class PathDataViewerBase
    {
        [Serializable]
        protected class ColorPathData
        {
            public Color Color;
            public bool Enabled = true;

            public Color GetColor()
            {
                var color = Color;

                if (!Enabled)
                {
                    color.a = 0;
                }

                return color;
            }
        }

        protected class WayInfo
        {
            public List<PathNode> Nodes = new List<PathNode>();
        }

        private const string ButtonOn = "animationvisibilitytoggleon";
        private const string ButtonOff = "animationvisibilitytoggleoff";
        private const float ButtonSize = 25f;
        private const float RemoveButtonSize = ButtonSize - 5;

        protected PathDataViewerWindow PathDataViewerWindow { get; private set; }

        public virtual void SwitchEnabledState(bool isEnabled) { }

        public virtual void Initialize(PathDataViewerWindow pathDataViewerWindow)
        {
            this.PathDataViewerWindow = pathDataViewerWindow;
        }

        public virtual void UpdateData(Path[] paths) { }

        public virtual void DrawCustomSettings() { }

        public virtual void LoadData() { }

        public virtual void SaveData() { }

        public virtual bool ShouldShowPathButton(Path path)
        {
            return true;
        }

        protected void DrawHideButton(Action onClick)
        {
            var tex = EditorGUIUtility.IconContent(ButtonOn);

            if (tex != null)
            {
                if (GUILayout.Button(tex, GUILayout.Width(ButtonSize)))
                {
                    onClick();
                }

                return;
            }

            if (GUILayout.Button("-", GUILayout.Width(ButtonSize)))
            {
                onClick();
            }
        }

        protected void DrawShowButton(Action onClick)
        {
            var tex = EditorGUIUtility.IconContent(ButtonOff);

            if (tex != null)
            {
                if (GUILayout.Button(tex, GUILayout.Width(ButtonSize)))
                {
                    onClick();
                }

                return;
            }

            if (GUILayout.Button("+", GUILayout.Width(ButtonSize)))
            {
                onClick();
            }
        }

        protected void DrawRemoveButton(Action onClick)
        {
            if (GUILayout.Button("X", GUILayout.Height(RemoveButtonSize), GUILayout.Width(RemoveButtonSize)))
            {
                onClick();
            }
        }

        protected void SetPathColor(Path path, Color color, bool state)
        {
            path.HasPathCustomColor = PathDataViewerWindow.DrawCustomColors && state;
        }
    }
}
#endif