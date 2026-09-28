using Spirit604.DotsCity.Simulation.Car.Custom.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.EditorTools
{
    [Serializable]
    public class CarPrefabInfo
    {
        public const int MaxLodCount = 3;

        public GameObject Prefab;
        public string Name;

        public IDSourceType IDSourceType;

        [Tooltip("New ID entry for vehicle collection")]
        public string ID;
        public string InternalID;
        public bool CustomID;

        public Mesh[] HullMeshLODs = new Mesh[MaxLodCount];
        public List<MeshRenderer> AllWheels = new List<MeshRenderer>();
        public List<MeshRenderer> ExtraWheels = new List<MeshRenderer>();
        public List<Mesh> AllWheelsMeshes = new List<Mesh>();
        public List<LODMeshData> LOD0Meshes = new List<LODMeshData>();
        public List<LODMeshData> LOD1Meshes = new List<LODMeshData>();
        public List<LODMeshData> LOD2Meshes = new List<LODMeshData>();
        public string[] AvailableWheels;
        public int SelectedWheelMeshIndex;
        public Mesh[] SharedWheelMesh = new Mesh[MaxLodCount];

        [Tooltip("How often the car will spawn (spawn weight)")]
        [Range(0, 1f)] public float Weight = 1f;

        public MeshRenderer SourceMesh;
        public Material CustomMaterial;

        [EnumPopup]
        public TrafficGroupType TrafficGroup = TrafficGroupType.Default;

        public bool OverrideEntityType;
        public EntityType EntityType;

        public bool PublicTransport;

        [Tooltip("The vehicle will only be spawned on TrafficPublicRoute paths")]
        public bool PredefinedRoad;

        [Range(0, 50)] public int Capacity = 20;
        [Range(0, 5)] public int Entries = 1;

        [Tooltip("" +
            "<b>New</b> : user-defined settings\r\n\r\n" +
            "<b>Template</b> : vehicle settings are copied from the selected template [Custom physics vehicle only]\r\n\r\n" +
            "<b>Clone model</b> : vehicle settings are copied from the selected `CarModel` in the prefab info list")]
        public SettingsType SettingsType;

        public string TemplateID;
        public string CloneID;
        public int LocalTemplateIndex;
        public VehicleCustomTemplate.CopySettingsType CopySettingsType = (VehicleCustomTemplate.CopySettingsType)~0;

        [Tooltip("Size offset of physics shape")]
        public VectorToggleable SizeOffset = new VectorToggleable("Size Offset");

        [Tooltip("Center offset of physics shape")]
        public VectorToggleable CenterOffset = new VectorToggleable("Center Offset");

        [Tooltip("Center of mass of the vehicle")]
        public VectorToggleable CenterOfMass = new VectorToggleable("Center Of Mass", new Vector3(0, 1f, 0.3f));

        [Tooltip("Bevel radius of physics shape")]
        public SliderToggleable BevelRadius = new SliderToggleable("Bevel Radius", 0.05f, 0f, 2f);

        [Tooltip("Mass of the vehicle")]
        public SliderToggleable Mass = new SliderToggleable("Mass", 1500f, 1f, 5000f);

        [Tooltip("Wheel radius")]
        public SliderToggleable WheelRadius = new SliderToggleable("Wheel Radius", 0f, 5f);

        [Tooltip("Wheel offset by Y-axis of the vehicle")]
        public SliderToggleable WheelOffset = new SliderToggleable("Wheel Offset", -5f, 5f);

        [Tooltip("Suspension length of the vehicle [Custom physics vehicles only]")]
        public SliderToggleable SuspensionLength = new SliderToggleable("Suspension Length", 0f, 3f);

        [Tooltip("Additional offset for non-physics mode for smooth transition between MonoBehaviour controller and non-physics mode (physics car can be offset to surface due to suspension & wheel things)")]
        public SliderToggleable AdditiveOffset = new SliderToggleable("Additive Offset", 0f, -1f, 1f);

        [Tooltip("Max steering angle of steering wheels")]
        public SliderToggleable MaxSteeringAngle = new SliderToggleable("Max Steering Angle", 40, 0f, 60f);

        [NonSerialized] public GameObject HullPrefab;
        [NonSerialized] public GameObject EntityPrefab;
        [NonSerialized] public float ResultWeight;

        public string CurrentInternalId => !string.IsNullOrEmpty(InternalID) ? InternalID : ID;

        public void CloneCache(CacheContainer.CachedData cachedData)
        {
            Name = cachedData.Name;

            if (cachedData.HasCustomID)
            {
                ID = cachedData.ID;
            }

            CustomID = cachedData.CustomID;
            IDSourceType = cachedData.IDSourceType;
            Weight = cachedData.Weight;
            TrafficGroup = cachedData.TrafficGroup;
            OverrideEntityType = cachedData.OverrideEntityType;
            EntityType = cachedData.EntityType;
            PublicTransport = cachedData.PublicTransport;
            Capacity = cachedData.Capacity;
            Entries = cachedData.Entries;

            SettingsType = cachedData.SettingsType;
            TemplateID = cachedData.TemplateID;
            CloneID = cachedData.CloneID;
            CopySettingsType = cachedData.CopySettingsType;

            if (cachedData.SizeOffset.Enabled)
            {
                SizeOffset.Value = cachedData.SizeOffset.Value;
                SizeOffset.Enabled = true;
            }

            if (cachedData.CenterOffset.Enabled)
            {
                CenterOffset.Value = cachedData.CenterOffset.Value;
                CenterOffset.Enabled = true;
            }

            if (cachedData.CenterOfMass.Enabled)
            {
                CenterOfMass.Value = cachedData.CenterOfMass.Value;
                CenterOfMass.Enabled = true;
            }

            if (cachedData.BevelRadius.Enabled)
            {
                BevelRadius.Value = cachedData.BevelRadius.Value;
                BevelRadius.Enabled = true;
            }

            if (cachedData.Mass.Enabled)
            {
                Mass.Value = cachedData.Mass.Value;
                Mass.Enabled = true;
            }

            if (cachedData.WheelRadius.Enabled)
            {
                WheelRadius.Value = cachedData.WheelRadius.Value;
                WheelRadius.Enabled = true;
            }

            if (cachedData.WheelOffset.Enabled)
            {
                WheelOffset.Value = cachedData.WheelOffset.Value;
                WheelOffset.Enabled = true;
            }

            if (cachedData.SuspensionLength.Enabled)
            {
                SuspensionLength.Value = cachedData.SuspensionLength.Value;
                SuspensionLength.Enabled = true;
            }

            if (cachedData.AdditiveOffset.Enabled)
            {
                AdditiveOffset.Value = cachedData.AdditiveOffset.Value;
                AdditiveOffset.Enabled = true;
            }

            if (cachedData.MaxSteeringAngle.Enabled)
            {
                MaxSteeringAngle.Value = cachedData.MaxSteeringAngle.Value;
                MaxSteeringAngle.Enabled = true;
            }

            AllWheelsMeshes = new List<Mesh>(cachedData.AllWheelsMeshes);
            LOD1Meshes = new List<LODMeshData>(cachedData.LOD1Meshes);
            LOD2Meshes = new List<LODMeshData>(cachedData.LOD2Meshes);
            AvailableWheels = cachedData.AvailableWheels;
            SelectedWheelMeshIndex = cachedData.SelectedWheelMeshIndex;

            if (cachedData.SharedWheelMesh != null)
            {
                SharedWheelMesh = new Mesh[cachedData.SharedWheelMesh.Length];
                Array.Copy(cachedData.SharedWheelMesh, SharedWheelMesh, cachedData.SharedWheelMesh.Length);
            }

            if (cachedData.HullMeshLODs != null)
            {
                HullMeshLODs = new Mesh[cachedData.HullMeshLODs.Length];
                Array.Copy(cachedData.HullMeshLODs, HullMeshLODs, cachedData.HullMeshLODs.Length);
            }
        }

        public void UpdateInternalID()
        {
            var meshName = string.Empty;

            if (SourceMesh != null)
            {
                meshName = SourceMesh.GetComponent<MeshFilter>()?.sharedMesh?.name ?? string.Empty;
            }

            InternalID = $"{Prefab.name}_{meshName}";
        }
    }
}
