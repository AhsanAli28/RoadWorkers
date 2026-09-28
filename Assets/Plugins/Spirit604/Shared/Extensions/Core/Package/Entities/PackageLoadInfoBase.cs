#if UNITY_EDITOR
using System;
using UnityEditor.PackageManager;

namespace Spirit604.PackageManagerExtension
{
    public abstract class PackageLoadInfoBase
    {
        public abstract bool IsCompleted { get; }
        public abstract bool IsRunning { get; }
        public abstract StatusCode Status { get; }
        public string ScriptDefine { get; set; }
        public bool HasScriptDefine => !string.IsNullOrEmpty(ScriptDefine);
        public Action OnInstall;

        public abstract void Load();
        public abstract string GetStatusMessage();
        public virtual void Tick() { }
        public virtual void Complete() { }
    }
}
#endif