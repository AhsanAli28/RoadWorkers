#if UNITY_EDITOR
using Spirit604.Extensions.Common;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    public class PackageManagerResolver
    {
        private static Queue<PackageLoadInfoBase> packageLoaderQueue = new Queue<PackageLoadInfoBase>();
        private static bool loadPackageAwait = false;
        private static ListRequest listRequest;
        private static Action<PackageCollection> onListResult;
        private static bool loadSuccess;
        private static Action<bool> onPackagesLoaded;

        public static void Load(IEnumerable<PackageLoadInfoBase> packages, Action<bool> onPackagesLoadedCallback = null)
        {
            foreach (var package in packages)
            {
                packageLoaderQueue.Enqueue(package);
            }

            if (!loadPackageAwait && packageLoaderQueue.Count > 0)
            {
                onPackagesLoaded = onPackagesLoadedCallback;
                loadPackageAwait = true;
                loadSuccess = true;
                EditorApplication.LockReloadAssemblies();
                EditorApplication.update += EditorApplication_update_addPackageRequest;
            }
        }

        public static void Load(IEnumerable<string> packages)
        {
            foreach (var package in packages)
            {
                var packageLoader = new PackageLoadInfo(package);
                packageLoaderQueue.Enqueue(packageLoader);
            }

            if (!loadPackageAwait && packageLoaderQueue.Count > 0)
            {
                loadPackageAwait = true;
                EditorApplication.LockReloadAssemblies();
                EditorApplication.update += EditorApplication_update_addPackageRequest;
            }
        }

        public static void List(Action<PackageCollection> onListResultCallback, bool offlineMode = true)
        {
            if (listRequest != null)
            {
                Dispose();
            }

            listRequest = Client.List(offlineMode, true);
            onListResult = onListResultCallback;
            EditorApplication.update += EditorApplication_update_listPackageRequest;
        }

        public static void Dispose()
        {
            if (listRequest != null)
            {
                EditorApplication.update -= EditorApplication_update_listPackageRequest;
                listRequest = null;
                onListResult = null;
            }
        }

        public static bool VersionIsValid(string minVersion, string currentVersion)
        {
            if (string.IsNullOrEmpty(minVersion))
            {
                return true;
            }

            var minVersionArray = GetVersions(minVersion, out var minSubversionText, out var minSubVersion);
            var currentVersionArray = GetVersions(currentVersion, out var currentSubversionText, out var currentSubVersion);

            if (minVersionArray != null && currentVersionArray != null && minVersionArray.Length > 0 && minVersionArray.Length == currentVersionArray.Length)
            {
                for (int i = 0; i < currentVersionArray.Length; i++)
                {
                    var num1 = System.Convert.ToInt32(minVersionArray[i]);
                    var num2 = System.Convert.ToInt32(currentVersionArray[i]);

                    if (num1 > num2)
                    {
                        return false;
                    }
                }

                if (!string.IsNullOrEmpty(minSubVersion) && !string.IsNullOrEmpty(currentSubVersion))
                {
                    if (minSubversionText == currentSubversionText)
                    {
                        var num1 = System.Convert.ToInt32(minSubVersion);
                        var num2 = System.Convert.ToInt32(currentSubVersion);

                        if (num1 > num2)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string[] GetVersions(string sourceVersion, out string subVersionText, out string subVersionValue)
        {
            string[] versionArray;
            subVersionText = string.Empty;
            subVersionValue = string.Empty;

            if (!sourceVersion.Contains("-"))
            {
                versionArray = sourceVersion.Split('.');
            }
            else
            {
                var currentVersionArr = sourceVersion.Split('-');
                versionArray = currentVersionArr[0].Split('.');

                if (currentVersionArr[1].Contains("."))
                {
                    var subVersionArray = currentVersionArr[1].Split('.');
                    subVersionText = subVersionArray[0];
                    subVersionValue = subVersionArray[1];
                }
                else
                {
                    subVersionText = currentVersionArr[1];
                }
            }

            return versionArray;
        }

        public static void TryToAddScriptDefine(string scriptDefine)
        {
            if (string.IsNullOrEmpty(scriptDefine))
            {
                return;
            }

            var added = PlayerBuildExtension.TryToAddDefineToCurrentBuilds(scriptDefine, false);

            if (added)
            {
                Debug.Log($"Script DEFINE '{scriptDefine}' added");
            }
        }

        private static void EditorApplication_update_addPackageRequest()
        {
            var packageLoader = packageLoaderQueue.Peek();

            if (!packageLoader.IsRunning)
            {
                packageLoader.Load();
            }

            packageLoader.Tick();

            if (packageLoader.IsCompleted)
            {
                if (packageLoader.Status == StatusCode.Success)
                {
                    if (packageLoader.HasScriptDefine)
                    {
                        TryToAddScriptDefine(packageLoader.ScriptDefine);
                    }

                    packageLoader.OnInstall?.Invoke();
                }
                else
                {
                    loadSuccess = false;
                }

                Debug.Log(packageLoader.GetStatusMessage());
                packageLoader.Complete();
                packageLoaderQueue.Dequeue();
            }

            if (packageLoaderQueue.Count == 0)
            {
                onPackagesLoaded?.Invoke(loadSuccess);
                loadPackageAwait = false;
                EditorApplication.update -= EditorApplication_update_addPackageRequest;
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void EditorApplication_update_listPackageRequest()
        {
            if (listRequest.IsCompleted)
            {
                onListResult?.Invoke(listRequest.Result);
                Dispose();
            }
        }
    }
}
#endif