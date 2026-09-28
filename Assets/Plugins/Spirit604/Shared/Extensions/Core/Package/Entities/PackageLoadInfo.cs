#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    public class PackageLoadInfo : PackageLoadInfoBase
    {
        private AddRequest addRequest;

        public string PackageName { get; private set; }

        public PackageLoadInfo(string packageName)
        {
            this.PackageName = packageName;
        }

        public override bool IsCompleted => addRequest != null && addRequest.IsCompleted;

        public override bool IsRunning => addRequest != null;

        public override StatusCode Status => addRequest != null ? addRequest.Status : StatusCode.InProgress;

        public override void Load()
        {
            Debug.Log($"Starting to load '{PackageName}' package");
            addRequest = Client.Add(PackageName);
        }

        public override string GetStatusMessage()
        {
            if (IsCompleted)
            {
                switch (Status)
                {
                    case StatusCode.Success:
                        return $"Package '{PackageName}' installed";
                    case StatusCode.Failure:
                        return $"Package '{PackageName}' failed to install. Reason: {addRequest.Error.message}";
                }
            }
            else if (IsRunning && Status == StatusCode.InProgress)
            {
                return $"Package '{PackageName}' is loading";
            }

            return $"Package '{PackageName}' loader not working";
        }
    }
}
#endif