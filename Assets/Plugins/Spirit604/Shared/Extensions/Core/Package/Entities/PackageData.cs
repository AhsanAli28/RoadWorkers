#if UNITY_EDITOR
namespace Spirit604.PackageManagerExtension
{
    public class PackageData : PackageDataBase
    {
        public string PackageLoadPath { get; set; }
        public string ScopeName { get; set; }
        public bool Scope { get; set; }

        public PackageData CurrentPackage => CurrentPackageBase as PackageData;
        public string CurrentPackageLoadPath => CurrentPackage.PackageLoadPath;
    }
}
#endif