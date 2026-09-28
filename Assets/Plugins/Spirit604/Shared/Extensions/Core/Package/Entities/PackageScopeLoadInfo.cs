#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    public class PackageScopeLoadInfo : PackageLoadInfoBase
    {
        private object addRequest;
        private PropertyInfo isCompletedProp;
        private PropertyInfo statusProp;
        private bool packageIsLoading;
        private bool packageIsComplete;
        private bool isRunning;
        private PackageLoadInfo relatedPackage;

        public string RegistryName { get; private set; }
        public string PackageName { get; private set; }
        public string Url { get; private set; }
        public string ScopeName { get; private set; }

        public PackageScopeLoadInfo(string registryName, string packageName, string url, string scope)
        {
            this.RegistryName = registryName;
            this.PackageName = packageName;
            this.Url = url;
            this.ScopeName = scope;
        }

        public override bool IsCompleted => ScopeIsCompleted && packageIsComplete;

        public override bool IsRunning => isRunning;

        private bool ScopeIsCompleted
        {
            get
            {
                if (addRequest != null)
                {
                    return (bool)isCompletedProp.GetValue(addRequest);
                }

                return false;
            }
        }

        public override StatusCode Status
        {
            get
            {
                if (relatedPackage != null)
                {
                    return relatedPackage.Status;
                }

                if (addRequest != null)
                {
                    return (StatusCode)statusProp.GetValue(addRequest);
                }

                return StatusCode.InProgress;
            }
        }

        public override void Load()
        {
            isRunning = true;
            Debug.Log($"Starting to load '{ScopeName}' scope");

            string[] scopes = new string[1];
            scopes[0] = ScopeName;

            object[] args = new object[3];
            args[0] = RegistryName;
            args[1] = Url;
            args[2] = scopes;

            var addScopedRegistryMethod = typeof(Client)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(x => x.Name == "AddScopedRegistry" && x.GetParameters().Length == 3)
            .FirstOrDefault();

            addRequest = addScopedRegistryMethod.Invoke(null, args);

            isCompletedProp = addRequest.GetType().GetProperties().Where(a => a.Name == "IsCompleted").FirstOrDefault();
            statusProp = addRequest.GetType().GetProperties().Where(a => a.Name == "Status").FirstOrDefault();
        }

        public override void Tick()
        {
            if (ScopeIsCompleted && !packageIsLoading)
            {
                packageIsLoading = true;
                bool success = true;

                if (Status == StatusCode.Failure)
                {
                    var error = GetError();

                    if (!error.message.Contains("used"))
                    {
                        success = false;
                    }
                }

                if (success)
                {
                    Debug.Log($"Scope '{ScopeName}' downloaded");
                    relatedPackage = new PackageLoadInfo(PackageName);
                    relatedPackage.Load();
                }
                else
                {
                    packageIsComplete = true;
                }
            }

            if (relatedPackage != null)
            {
                if (relatedPackage.IsCompleted)
                {
                    packageIsComplete = true;
                }
            }
        }

        public override string GetStatusMessage()
        {
            if (relatedPackage != null)
            {
                return relatedPackage.GetStatusMessage();
            }

            if (IsCompleted)
            {
                switch (Status)
                {
                    case StatusCode.Success:
                        return $"Scope '{ScopeName}' installed";
                    case StatusCode.Failure:
                        return $"{GetError().message}";
                }
            }
            else if (IsRunning && Status == StatusCode.InProgress)
            {
                return $"Scope '{ScopeName}' is loading";
            }

            return $"Package '{RegistryName}' loader not working";
        }

        private UnityEditor.PackageManager.Error GetError()
        {
            return addRequest.GetType().GetProperty("Error").GetValue(addRequest) as UnityEditor.PackageManager.Error;
        }
    }
}
#endif