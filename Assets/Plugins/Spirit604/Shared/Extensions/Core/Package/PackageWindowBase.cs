#if UNITY_EDITOR
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    public abstract class PackageWindowBase : EditorWindowBase
    {
        protected Dictionary<string, PackageData> requirePackages = new Dictionary<string, PackageData>();
        protected Dictionary<string, PackageData> optionalPackages = new Dictionary<string, PackageData>();
        protected Dictionary<string, AssetStorePackage> assetStorePackages = new Dictionary<string, AssetStorePackage>();
        protected List<string> requireScriptDefines = new List<string>();
        protected bool loadingPackageData = true;
        protected bool loadingPackages = false;

        protected virtual float LabelWidth => 320f;
        protected virtual bool HasCustomInitTab => false;
        protected virtual string CustomTabHeader => string.Empty;

        protected override Vector2 GetDefaultWindowSize() => new Vector2(400, 300);
        protected bool HasRequiredScriptDefines => requireScriptDefines != null && requireScriptDefines.Count > 0;
        protected bool HasOptionalPackages => optionalPackages != null && optionalPackages.Keys.Count > 0;
        protected bool HasAssetStorePackages => assetStorePackages != null && assetStorePackages.Keys.Count > 0;

        protected override void OnEnable()
        {
            base.OnEnable();

            loadingPackages = false;
            PopulateRequirePackageData();
            PackageManagerResolver.List(PackageLoader_OnListResult);
            SearchLocalAssetStorePackages();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PackageManagerResolver.Dispose();
        }

        protected virtual void OnGUI()
        {
            ShowPackageGUI();
        }

        protected virtual void PopulateRequirePackageData()
        {
            requirePackages = LoadRequirePackageData();
            requireScriptDefines = LoadRequireScriptDefines();
            optionalPackages = LoadOptionalPackageData();
            assetStorePackages = LoadAssetStorePackageData();
        }

        protected abstract Dictionary<string, PackageData> LoadRequirePackageData();
        protected virtual List<string> LoadRequireScriptDefines() { return null; }
        protected virtual Dictionary<string, PackageData> LoadOptionalPackageData() { return null; }
        protected virtual Dictionary<string, AssetStorePackage> LoadAssetStorePackageData() { return null; }

        protected virtual void ShowPackageGUI()
        {
            if (loadingPackageData)
            {
                EditorGUILayout.LabelField("Loading package data...", EditorStyles.boldLabel);
            }
            else
            {
                ShowPackageInfo();
            }
        }

        protected void ShowPackageInfo()
        {
            InspectorExtension.DrawGroupBox("Required Packages:", () =>
            {
                foreach (var item in requirePackages)
                {
                    ShowPackageInfo(item.Value);
                }

                if (HasRequiredScriptDefines)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.LabelField("Required script defines:", EditorStyles.boldLabel);

                    foreach (var requireScriptDefine in requireScriptDefines)
                    {
                        EditorGUILayout.LabelField(requireScriptDefine, EditorStyles.boldLabel);
                    }
                }

                if (!loadingPackages)
                {
                    if (GUILayout.Button("Load Packages"))
                    {
                        var packagesToLoad = GetPackagesToLoad(true);

                        if (packagesToLoad?.Count > 0)
                        {
                            loadingPackages = true;
                            PackageManagerResolver.Load(packagesToLoad, PackageManagerResolver_onPackagesLoaded);
                        }
                    }
                }
            });

            if (HasOptionalPackages)
            {
                InspectorExtension.DrawGroupBox("Optional Packages:", () =>
                {
                    foreach (var item in optionalPackages)
                    {
                        ShowPackageInfo(item.Value);
                    }

                    if (!loadingPackages)
                    {
                        if (HasOptionalPackages)
                        {
                            if (GUILayout.Button("Load Optional Packages"))
                            {
                                BeforeOptionalLoading();
                                var packagesToLoad = GetPackagesToLoad(true, false);

                                if (packagesToLoad?.Count > 0)
                                {
                                    loadingPackages = true;
                                    PackageManagerResolver.Load(packagesToLoad, PackageManagerResolver_onPackagesLoaded);
                                }
                            }
                        }
                    }
                });
            }

            if (HasAssetStorePackages)
            {
                InspectorExtension.DrawGroupBox("Asset Store Packages:", () =>
                {
                    foreach (var item in assetStorePackages)
                    {
                        ShowPackageInfo(item.Value);
                    }
                });
            }

            if (HasCustomInitTab)
            {
                InspectorExtension.DrawGroupBox(CustomTabHeader, () =>
                {
                    ShowCustomTab();
                });
            }

            if (loadingPackages)
            {
                GUI.enabled = false;
                GUILayout.Button("Loading...");
                GUI.enabled = true;
            }
        }

        private void ShowPackageInfo(PackageDataBase requirePackage)
        {
            if (requirePackage.PackageVariant) return;

            EditorGUILayout.BeginHorizontal();

            var packageText = $"Package '{requirePackage.PackageName}'";

            if (!HasFoldout(requirePackage))
            {
                if (!requirePackage.HasVariants)
                {
                    EditorGUILayout.LabelField(packageText, GUILayout.MinWidth(LabelWidth));
                }
                else
                {
                    var popups = requirePackage.Variants.Select(a => a.PackageName).ToArray();

                    for (int i = 0; i < popups.Length; i++)
                    {
                        popups[i] = $"Package '{popups[i]}'";
                    }

                    var variantIndex = requirePackage.VariantIndex;
                    var newIndex = EditorGUILayout.Popup(variantIndex, popups, GUILayout.MinWidth(LabelWidth - 30));

                    if (variantIndex != newIndex)
                    {
                        requirePackage.VariantIndex = newIndex;
                        CheckForInstalledDefines();
                    }

                    EditorGUILayout.LabelField(string.Empty, GUILayout.MinWidth(27));
                }
            }
            else
            {
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(LabelWidth));
                requirePackage.FoldoutFlag = EditorGUILayout.Foldout(requirePackage.FoldoutFlag, packageText);
                EditorGUILayout.EndVertical();
            }

            ShowInstallStatus(requirePackage.CurrentPackageBase);

            EditorGUILayout.EndHorizontal();

            var isAssetStorePackage = requirePackage is AssetStorePackage;

            if (HasFoldout(requirePackage) && requirePackage.FoldoutFlag)
            {
                EditorGUI.indentLevel++;

                if (requirePackage.HasCustomScriptDefine)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($"Script define:", GUILayout.MinWidth(LabelWidth - 12f));
                    EditorGUILayout.LabelField(requirePackage.CustomScriptDefine, EditorStyles.boldLabel);

                    EditorGUILayout.EndHorizontal();
                }

                if (requirePackage.HasDescription)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField(string.Empty, GUILayout.Width(10));

                    if (!requirePackage.HasDescriptionUrl)
                    {
                        EditorGUILayout.LabelField(requirePackage.Description);
                    }
                    else
                    {
#if UNITY_2021_1_OR_NEWER

                        if (EditorGUILayout.LinkButton(requirePackage.Description))
                        {
                            Application.OpenURL(requirePackage.DescriptionUrl);
                        }
#else
                        if (GUILayout.Button(requirePackage.Description, GUI.skin.label))
                        {
                            Application.OpenURL(requirePackage.DescriptionUrl);
                        }
#endif
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (isAssetStorePackage)
                {
                    var assetStorePackage = requirePackage as AssetStorePackage;

                    for (int i = 0; i < assetStorePackage.AssetDownloadDatas?.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(string.Empty, GUILayout.Width(10));

                        AssetDownloadData assetDownloadData = assetStorePackage.AssetDownloadDatas[i];

#if UNITY_2021_1_OR_NEWER

                        if (EditorGUILayout.LinkButton(assetDownloadData.Description))
                        {
                            Application.OpenURL(assetDownloadData.DownloadUrl);
                        }
#else
                        if (GUILayout.Button(assetDownloadData.Description, GUI.skin.label))
                        {
                            Application.OpenURL(assetDownloadData.DownloadUrl);
                        }
#endif

                        EditorGUILayout.EndHorizontal();

                    }
                }

                EditorGUI.indentLevel--;
            }

            if (isAssetStorePackage)
            {
                if (requirePackage.HasCustomScriptDefine)
                {
                    GUI.enabled = requirePackage.InstallStatus == InstallStatus.Installed;

                    if (GUILayout.Button("Add Scriping Define"))
                    {
                        PackageManagerResolver.TryToAddScriptDefine(requirePackage.CustomScriptDefine);
                    }

                    GUI.enabled = true;
                }
            }
        }

        protected virtual void BeforeOptionalLoading() { }

        protected virtual void ShowCustomTab()
        {
            throw new NotImplementedException();
        }

        protected virtual void PostProcessLoadedPackages(bool isLoaded) { }

        protected void ShowInstallStatus(PackageDataBase packageData)
        {
            if (packageData.Optional)
            {
                var lastRect = GUILayoutUtility.GetLastRect();

                lastRect.x += lastRect.width - 16;
                lastRect.y += 3f;
                lastRect.width = 25f;
                var installFlag = packageData.Install;
                var newInstallFlag = EditorGUI.Toggle(lastRect, installFlag);

                if (newInstallFlag != installFlag)
                {
                    packageData.Install = newInstallFlag;
                }

                if (!packageData.Install)
                {
                    EditorGUILayout.LabelField($"Ignored", EditorStyles.boldLabel);
                    return;
                }
            }

            var installStatus = packageData.InstallStatus;

            switch (installStatus)
            {
                case InstallStatus.NotInstalled:
                    {
                        string versionText = string.Empty;
                        bool hasMinVersion = packageData.HasMinVersion;

                        if (hasMinVersion)
                        {
                            versionText = $" ({packageData.MinVersion})";
                        }

                        EditorGUILayout.LabelField($"Not Installed{versionText}", EditorStyles.boldLabel);
                        break;
                    }
                case InstallStatus.Installed:
                    {
                        string versionText = string.Empty;
                        bool hasCurrentVersion = packageData.HasCurrentVersion;

                        if (hasCurrentVersion)
                        {
                            versionText = $" ({packageData.CurrentVersion})";
                        }

                        EditorGUILayout.LabelField($"Installed{versionText}", EditorStyles.boldLabel);
                        break;
                    }
                case InstallStatus.OutOfVersion:
                    {
                        var text = $"Outdated ({packageData.CurrentVersion}) - {packageData.MinVersion}";
                        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
                        break;
                    }
            }
        }

        private bool HasFoldout(PackageDataBase requirePackage)
        {
            return requirePackage.HasDescription || requirePackage.HasCustomScriptDefine;
        }

        protected List<PackageLoadInfoBase> GetPackagesToLoad(bool autoAddScriptDefine = false, bool requiredPackages = true)
        {
            var packagesToLoad = new List<PackageLoadInfoBase>();

            var packageSource = requiredPackages ? requirePackages : optionalPackages;

            foreach (var requirePackage in packageSource)
            {
                var mainPackage = requirePackage.Value;
                var currentPackage = mainPackage.CurrentPackage;

                if (currentPackage.InstallStatus != InstallStatus.Installed)
                {
                    if (currentPackage.Optional && !currentPackage.Install || mainPackage.PackageVariant)
                        continue;

                    if (!currentPackage.Scope)
                    {
                        packagesToLoad.TryToAdd(new PackageLoadInfo(currentPackage.PackageLoadPath)
                        {
                            ScriptDefine = currentPackage.CustomScriptDefine,
                            OnInstall = currentPackage.OnPackageInstall
                        });
                    }
                    else
                    {
                        var registryName = requirePackage.Key;
                        var packageName = currentPackage.PackageName;
                        var url = currentPackage.PackageLoadPath;
                        var scope = currentPackage.ScopeName;

                        packagesToLoad.TryToAdd(new PackageScopeLoadInfo(registryName, packageName, url, scope)
                        {
                            ScriptDefine = currentPackage.CustomScriptDefine,
                        });
                    }
                }
                else if (autoAddScriptDefine)
                {
                    if (currentPackage.Install)
                        PackageManagerResolver.TryToAddScriptDefine(currentPackage.CustomScriptDefine);
                }
            }

            return packagesToLoad;
        }

        protected void ForEach(Action<PackageData> iterate)
        {
            foreach (var requirePackage in requirePackages)
            {
                if (requirePackage.Value.PackageVariant) continue;

                iterate(requirePackage.Value.CurrentPackage);
            }
        }

        protected bool CheckForPackagesInstallation()
        {
            var allInstalled = true;

            ForEach(currentPackage =>
            {
                bool installed = false;

                if (currentPackage.InstallStatus != InstallStatus.Installed)
                {
                    bool packageInstalled = false;
                    if (currentPackage.HasAssembly)
                    {
                        if (HasAssembly(currentPackage.TargetFileToFind))
                        {
                            installed = true;
                            currentPackage.InstallStatus = InstallStatus.Installed;
                            packageInstalled = true;
                        }
                    }

                    if (!packageInstalled && currentPackage.Optional && !currentPackage.Install)
                    {
                        installed = true;
                        packageInstalled = true;
                    }

                    if (!packageInstalled)
                    {
                        currentPackage.Initialized = false;
                    }
                }
                else
                {
                    installed = true;
                }

                if (!installed)
                {
                    allInstalled = false;
                }
            });

            return allInstalled;
        }

        protected void CheckForInstalledDefines()
        {
            var allInstalled = CheckForPackagesInstallation();

            if (allInstalled)
            {
                PostProcessLoadedPackages(true);

                var userDefines = LoadRequireScriptDefines();

                for (int i = 0; i < userDefines?.Count; i++)
                {
                    string userDefine = userDefines[i];
                    PackageManagerResolver.TryToAddScriptDefine(userDefine);
                }

                foreach (var requirePackage in requirePackages)
                {
                    if (requirePackage.Value.HasCustomScriptDefine)
                    {
                        PackageManagerResolver.TryToAddScriptDefine(requirePackage.Value.CustomScriptDefine);
                    }
                }
            }
            else
            {
                PostProcessLoadedPackages(false);
            }
        }

        private void AddPackage(Dictionary<string, PackageData> requirePackages, UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            if (requirePackages == null || !requirePackages.ContainsKey(packageInfo.name))
                return;

            var package = requirePackages[packageInfo.name];
            package.CurrentVersion = packageInfo.version;

            var validVersion = PackageManagerResolver.VersionIsValid(package.MinVersion, packageInfo.version);

            package.InstallStatus = validVersion ? InstallStatus.Installed : InstallStatus.OutOfVersion;

            if (package.HasVariants)
            {
                if (!package.VariantVersions)
                {
                    var variant = package.GetVariant(packageInfo.name);

                    if (variant != null)
                    {
                        variant.InstallStatus = package.InstallStatus;
                    }
                }
                else
                {
                    for (int i = 0; i < package.Variants.Count; i++)
                    {
                        var variant = package.Variants[i];
                        var installed = PackageManagerResolver.VersionIsValid(variant.MinVersion, packageInfo.version);

                        variant.InstallStatus = installed ? InstallStatus.Installed : InstallStatus.NotInstalled;
                    }
                }
            }
        }

        private void SearchLocalAssetStorePackages()
        {
            if (!HasAssetStorePackages)
                return;

            foreach (var assetStorePackage in assetStorePackages)
            {
                if (assetStorePackage.Value.HasAssembly)
                {
                    if (HasAssembly(assetStorePackage.Value.TargetFileToFind))
                    {
                        assetStorePackage.Value.InstallStatus = InstallStatus.Installed;
                    }
                }
            }
        }

        private bool HasAssembly(string assemblyName)
        {
            var assets = AssetDatabase.FindAssets($"{assemblyName}");

            if (assets?.Length > 0)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(assets[i]);

                    if (!string.IsNullOrEmpty(path) && path.Contains("asmdef"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual void PackageLoader_OnListResult(PackageCollection packageCollection)
        {
            foreach (var packageInfo in packageCollection)
            {
                AddPackage(requirePackages, packageInfo);
                AddPackage(optionalPackages, packageInfo);
            }

            CheckForInstalledDefines();

            loadingPackageData = false;
            Repaint();
        }

        private void PackageManagerResolver_onPackagesLoaded(bool success)
        {
            loadingPackages = false;

            if (success)
            {
                CheckForInstalledDefines();
            }
        }
    }
}
#endif