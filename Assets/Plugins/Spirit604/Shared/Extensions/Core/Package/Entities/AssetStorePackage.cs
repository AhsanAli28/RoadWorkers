#if UNITY_EDITOR

using System.Collections.Generic;

namespace Spirit604.PackageManagerExtension
{
    public class AssetStorePackage : PackageDataBase
    {
        public List<AssetDownloadData> AssetDownloadDatas { get; set; } = new List<AssetDownloadData>();
    }

    public class AssetDownloadData
    {
        public string DownloadUrl { get; set; }
        public string Description { get; set; }
        public bool DescriptionFoldout { get; set; } = true;
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }
}
#endif