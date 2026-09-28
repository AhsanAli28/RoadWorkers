#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    public class PackageDataBase
    {
        public string PackageName { get; set; }
        public string MinVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string CustomScriptDefine { get; set; }

        public InstallStatus InstallStatus
        {
            get => installStatus; set
            {
                installStatus = value;

                if (value == InstallStatus.NotInstalled)
                {
                    Initialized = false;
                }
            }
        }
        public string Description { get; set; }
        public bool FoldoutFlag { get; set; }
        public string DescriptionUrl { get; set; }
        public string TargetFileToFind { get; set; }
        public bool Optional { get; set; }
        public bool VariantVersions { get; set; }

        public bool Initialized
        {
            get => EditorPrefs.GetBool(GetKey("Initialized", true), false);
            set
            {
                EditorPrefs.SetBool(GetKey("Initialized", true), value);
            }
        }

        public Action OnPackageInstall;
        public Func<bool> OnProjectInstall;

        public bool Install
        {
            get => EditorPrefs.GetBool(GetKey("Install"), true);
            set => EditorPrefs.SetBool(GetKey("Install"), value);
        }

        public int DefaultIndex { get; set; }
        public bool PackageVariant;

        public int VariantIndex
        {
            get => EditorPrefs.GetInt(GetKey("VariantIndex"), DefaultIndex);
            set => EditorPrefs.SetInt(GetKey("VariantIndex"), value);
        }

        public List<PackageDataBase> Variants;
        private InstallStatus installStatus;

        public string CurrentPackageName => CurrentPackageBase.PackageName;
        public PackageDataBase CurrentPackageBase => !HasVariants ? this : Variants[VariantIndex];

        public bool HasCustomScriptDefine => !string.IsNullOrEmpty(CustomScriptDefine);
        public bool HasMinVersion => !string.IsNullOrEmpty(MinVersion);
        public bool HasCurrentVersion => !string.IsNullOrEmpty(CurrentVersion);
        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public bool HasDescriptionUrl => !string.IsNullOrEmpty(DescriptionUrl);
        public bool HasAssembly => !string.IsNullOrEmpty(TargetFileToFind);
        public bool HasVariants => Variants != null && Variants.Count > 0;

        public PackageDataBase GetVariant(string packageName) => HasVariants ? Variants.Find(x => x.PackageName == packageName) : null;

        public void RaiseProjectInstall()
        {
            if (!Initialized)
            {
                if (OnProjectInstall != null)
                {
                    Initialized = OnProjectInstall.Invoke();
                }
            }
        }

        private string GetKey(string propertyKey, bool uniqueKey = false) =>
            uniqueKey ?
            $"{Application.dataPath.GetHashCode()}_{PackageName}_{propertyKey}" :
            $"{PackageName}_{propertyKey}";
    }
}
#endif