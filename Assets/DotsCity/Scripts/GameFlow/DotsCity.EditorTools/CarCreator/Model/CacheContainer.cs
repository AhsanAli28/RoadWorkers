using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Car.Custom.Authoring;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.EditorTools
{
    [CreateAssetMenu(fileName = "Cache Container", menuName = CityEditorBookmarks.CITY_EDITOR_TRAFFIC_EDITOR_CONFIGS_PATH + "Cache Container")]
    public class CacheContainer : ScriptableObjectBase
    {
        [Serializable]
        public class CachedData : ICloneable
        {
            public string Name;

            public string ID;
            public bool CustomID;
            public IDSourceType IDSourceType;

            [Tooltip("How often the car will spawn (spawn weight)")]
            public float Weight;
            public TrafficGroupType TrafficGroup;

            public bool OverrideEntityType;
            public EntityType EntityType;

            public bool PublicTransport;

            [Tooltip("The vehicle will only be spawned on TrafficPublicRoute paths")]
            public bool PredefinedRoad;

            public int Capacity;
            public int Entries;

            public List<Mesh> AllWheelsMeshes = new List<Mesh>();

            public Mesh[] HullMeshLODs = new Mesh[3];
            public Mesh[] SharedWheelMesh = new Mesh[3];

            public List<LODMeshData> LOD1Meshes = new List<LODMeshData>();
            public List<LODMeshData> LOD2Meshes = new List<LODMeshData>();
            public string[] AvailableWheels;
            public int SelectedWheelMeshIndex;

            [Tooltip("" +
                "<b>New</b> : user-defined settings\r\n\r\n" +
                "<b>Template</b> : vehicle settings are copied from the selected template [Custom physics vehicle only]\r\n\r\n" +
                "<b>Clone model</b> : vehicle settings are copied from the selected `CarModel` in the prefab info list")]
            public SettingsType SettingsType;

            public string TemplateID;
            public string CloneID;
            public VehicleCustomTemplate.CopySettingsType CopySettingsType;

            [Tooltip("Size offset of physics shape")]
            public VectorToggleable SizeOffset;

            [Tooltip("Center offset of physics shape")]
            public VectorToggleable CenterOffset;

            [Tooltip("Center of mass of the vehicle")]
            public VectorToggleable CenterOfMass;

            [Tooltip("Bevel radius of physics shape")]
            public SliderToggleable BevelRadius;

            [Tooltip("Mass of the vehicle")]
            public SliderToggleable Mass;

            [Tooltip("Wheel radiuss")]
            public SliderToggleable WheelRadius;

            [Tooltip("Wheel offset by Y-axis of the vehicle")]
            public SliderToggleable WheelOffset;

            [Tooltip("Suspension length of the vehicle [Custom physics vehicles only]")]
            public SliderToggleable SuspensionLength;

            [Tooltip("Additional offset for non-physics mode for smooth transition between MonoBehaviour controller and non-physics mode (physics car can be offset to surface due to suspension & wheel things)")]
            public SliderToggleable AdditiveOffset;

            [Tooltip("Max steering angle of steering wheels")]
            public SliderToggleable MaxSteeringAngle;

            public bool HasCustomID =>
                !string.IsNullOrEmpty(ID) &&
                CustomID &&
                (IDSourceType == IDSourceType.Custom ||
                IDSourceType == IDSourceType.Collection);

            public CachedData(CarPrefabInfo carPrefabInfo)
            {
                Name = carPrefabInfo.Name;
                ID = carPrefabInfo.ID;
                CustomID = carPrefabInfo.CustomID;
                IDSourceType = carPrefabInfo.IDSourceType;
                Weight = carPrefabInfo.Weight;
                TrafficGroup = carPrefabInfo.TrafficGroup;
                OverrideEntityType = carPrefabInfo.OverrideEntityType;
                EntityType = carPrefabInfo.EntityType;
                PublicTransport = carPrefabInfo.PublicTransport;
                PredefinedRoad = carPrefabInfo.PredefinedRoad;
                Capacity = carPrefabInfo.Capacity;
                Entries = carPrefabInfo.Entries;
                TemplateID = carPrefabInfo.TemplateID;
                CloneID = carPrefabInfo.CloneID;
                CopySettingsType = carPrefabInfo.CopySettingsType;
                SettingsType = carPrefabInfo.SettingsType;
                SizeOffset = carPrefabInfo.SizeOffset;
                CenterOffset = carPrefabInfo.CenterOffset;
                CenterOfMass = carPrefabInfo.CenterOfMass;
                BevelRadius = carPrefabInfo.BevelRadius;
                Mass = carPrefabInfo.Mass;
                WheelRadius = carPrefabInfo.WheelRadius;
                WheelOffset = carPrefabInfo.WheelOffset;
                SuspensionLength = carPrefabInfo.SuspensionLength;
                AdditiveOffset = carPrefabInfo.AdditiveOffset;
                MaxSteeringAngle = carPrefabInfo.MaxSteeringAngle;

                AllWheelsMeshes = new List<Mesh>(carPrefabInfo.AllWheelsMeshes);
                LOD1Meshes = new List<LODMeshData>(carPrefabInfo.LOD1Meshes);
                LOD2Meshes = new List<LODMeshData>(carPrefabInfo.LOD2Meshes);
                SelectedWheelMeshIndex = carPrefabInfo.SelectedWheelMeshIndex;

                AvailableWheels = new string[carPrefabInfo.AvailableWheels.Length];
                Array.Copy(carPrefabInfo.AvailableWheels, AvailableWheels, carPrefabInfo.AvailableWheels.Length);

                SharedWheelMesh = new Mesh[carPrefabInfo.SharedWheelMesh.Length];
                Array.Copy(carPrefabInfo.SharedWheelMesh, SharedWheelMesh, carPrefabInfo.SharedWheelMesh.Length);

                HullMeshLODs = new Mesh[carPrefabInfo.HullMeshLODs.Length];
                Array.Copy(carPrefabInfo.HullMeshLODs, HullMeshLODs, carPrefabInfo.HullMeshLODs.Length);
            }

            public object Clone()
            {
                return this.MemberwiseClone();
            }

            public CachedData GetClone()
            {
                return this.Clone() as CachedData;
            }
        }

        // Id - CachedData
        [Serializable]
        public class CachedDataDictionary : AbstractSerializableDictionary<string, CachedData> { }

        [Tooltip("Selected prefabs from the project")]
        public List<GameObject> Prefabs = new List<GameObject>();

        public MaterialType materialType;

        [Tooltip("Custom material for created vehicles")]
        public Material CustomMaterial;

        [Tooltip("Add the created prefabs to an existing preset on the scene")]
        public bool addToExistPreset = true;

        public PresetSourceType presetSourceType;

        [Tooltip("Selected preset for created vehicles")]
        public TrafficCarPoolPreset SelectedPreset;

        [Tooltip("Selected preset for created vehicles")]
        public TrafficCarPoolPreset SelectedPresetCustom;

        [Tooltip("Selected preset for created vehicles")]
        public TrafficCarPoolPreset SelectedPresetMono;

        [Tooltip("Selected preset for created player vehicles")]
        public TrafficCarPoolPreset SelectedPlayerPreset;

        [Tooltip("Script that implements 'IVehicleInput' interface to link traffic input & your vehicle controller input")]
        public MonoScript InputScript;

        public ScanIDSourceType scanIDSourceType = ScanIDSourceType.PrefabName;
        public WheelSearchType wheelSearchType = WheelSearchType.ByTextPattern | WheelSearchType.ByPosition;
        public ColliderType colliderType = ColliderType.Box;

        [Tooltip("Physical shape will be resized to the mesh size")]
        public bool fitPhysicsShapeToMesh = true;
        public bool includeWheels;
        public bool physicsShapeAtFloor;

        [Tooltip("Should search for wheels on a template")]
        public bool hasWheels = true;

        [Tooltip("Offset of the vehicle hull along the Y axis")]
        public bool addOffset = true;

        [Tooltip("Automatically positions the hull pivot point in the lower hull plane")]
        public bool fixPivot = true;

        [Tooltip("Offsets the local position of the body by the wheel radius")]
        public bool addWheelOffset = true;

        [Tooltip("Additional offset")]
        [Range(-2f, 2f)] public float localOffset;

        [Tooltip("Offset of the vehicle hull along the Y axis")]
        public bool addOffsetCustom;

        [Tooltip("Automatically positions the hull pivot point in the lower hull plane")]
        public bool fixPivotCustom = true;

        [Tooltip("Offsets the local position of the body by the wheel radius")]
        public bool addWheelOffsetCustom = true;

        [Tooltip("Additional offset")]
        [Range(-2f, 2f)] public float localOffsetCustom;

        public string templateNamePlayerHull = "_player_hull";
        public string templateNameTrafficHull = "_hull";
        public string templateNamePlayer = "_player";
        public string templateNameSimple = "_simple";
        public string templateNameCustom = "_custom";

        [Range(0f, 5f)] public float wheelRadius;

        [Tooltip("Wheel offset by Y-axis of the vehicle")]
        [Range(-5f, 5f)] public float wheelOffset;

        [Tooltip("Wheel offset by Y-axis of the vehicle")]
        [Range(-5f, 5f)] public float wheelOffsetCustom;

        [Range(0f, 5f)] public float suspensionLength = VehicleAuthoring.DefaultSuspensionLength;

        [Tooltip("Additional offset for non-physics mode for smooth transition between MonoBehaviour controller and non-physics mode (physics car can be offset to surface due to suspension & wheel things)")]
        [Range(-1, 1f)] public float additiveOffset;

        [Tooltip("Max steering angle of steering wheels")]
        [Range(0, 60f)] public float maxSteeringAngle = 40;

        [Tooltip("Size offset of physics shape")]
        public Vector3 SizeOffset;

        [Tooltip("Center offset of physics shape")]
        public Vector3 CenterOffset;

        [Tooltip("Center of mass of the vehicle")]
        public Vector3 CenterOfMass = new Vector3(0, 1f, 0.3f);

        [Tooltip("Mass of the vehicle")]
        [Range(1f, 5000f)] public float Mass = 200f;

        [Tooltip("Mass of the vehicle")]
        [Range(1f, 5000f)] public float MassCustom = 1500f;

        [Tooltip("Bevel radius of physics shape")]
        [Range(0f, 2f)] public float BevelRadius = 0.05f;

        [Tooltip("" +
            "<b>Model unique</b> : the wheels remain as in the original model\r\n\r\n" +
            "<b>Shared from model</b> : the wheel model selected by the user from the original model is used for all wheels\r\n\r\n" +
            "<b>Shared all</b> : the wheel model selected by the user shared between all wheels")]
        public WheelMeshSourceType WheelSourceType;

        [Tooltip("" +
            "<b>Source</b> : the wheel rotation remains unchanged\r\n\r\n" +
            "<b>Flip left row</b> : rotate the wheel in the left-hand row by 180° if you are using the wheel model from the right-hand row\r\n\r\n" +
            "<b>Flip right row</b> : rotate the wheel in the right-hand row by 180° if you are using the wheel model from the left-hand row")]
        public WheelRotationType WheelRotationType;

        public Mesh SharedWheelMesh;
        public Mesh SharedWheelMeshLOD1;
        public Mesh SharedWheelMeshLOD2;

        [Tooltip("On/off LODs for vehicle")]
        public bool HasLods;
        public float Lod0ScreenSize = 1.2f;
        public float Lod1ScreenSize = 0.3f;
        public float Lod2ScreenSize = 0f;

        [SerializeField]
        private CachedDataDictionary cachedDataDictionary = new CachedDataDictionary();

        public float WheelRadius
        {
            get => wheelRadius;
            set
            {
                if (wheelRadius != value)
                {
                    wheelRadius = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public float SuspensionLength
        {
            get => suspensionLength;
            set
            {
                if (suspensionLength != value)
                {
                    suspensionLength = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public float AdditiveOffset
        {
            get => additiveOffset;
            set
            {
                if (additiveOffset != value)
                {
                    additiveOffset = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public float MaxSteeringAngle
        {
            get => maxSteeringAngle;
            set
            {
                if (maxSteeringAngle != value)
                {
                    maxSteeringAngle = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public bool GetAddOfset(EntityType entityType) => IsCustom(entityType) ? addOffsetCustom : addOffset;

        public bool GetFixPivot(EntityType entityType) => IsCustom(entityType) ? fixPivotCustom : fixPivot;

        public bool GetAddWheelOffset(EntityType entityType) => IsCustom(entityType) ? addWheelOffsetCustom : addWheelOffset;

        public float GetLocalOffset(EntityType entityType) => IsCustom(entityType) ? localOffsetCustom : localOffset;

        public float GetWheelOffset(EntityType entityType) => IsCustom(entityType) ? wheelOffsetCustom : wheelOffset;

        public float SetWheelOffset(EntityType entityType, float value) => IsCustom(entityType) ? wheelOffsetCustom = value : wheelOffset = value;

        public float GetMass(EntityType entityType) => IsCustom(entityType) ? MassCustom : Mass;

        public float SetMass(EntityType entityType, float value) => IsCustom(entityType) ? MassCustom = value : Mass = value;

        public bool IncludeWheelsAvailable(EntityType entityType) => !IsCustom(entityType);

        public bool IncludeWheels(EntityType entityType) => includeWheels && IncludeWheelsAvailable(entityType);

        public TrafficCarPoolPreset GetSelectedPreset(EntityType entityType, bool isPlayer)
        {
            if (!isPlayer)
            {
                if (IsCustom(entityType))
                {
                    return SelectedPresetCustom;
                }
                else if (IsMono(entityType))
                {
                    return SelectedPresetMono;
                }
                else
                {
                    return SelectedPreset;
                }
            }

            return SelectedPlayerPreset;
        }

        public string GetTemplateName(EntityType entityType, bool isPlayer, bool hull)
        {
            if (hull)
            {
                if (isPlayer)
                {
                    return templateNamePlayerHull;
                }
                else
                {
                    return templateNameTrafficHull;
                }
            }

            if (isPlayer)
            {
                return templateNamePlayer;
            }

            if (IsCustom(entityType))
            {
                return templateNameCustom;
            }

            return templateNameSimple;
        }

        public void DrawCommonSettings(SerializedObject so, EntityType entityType)
        {
            DrawCommonSettingsForAll(so, entityType);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(fitPhysicsShapeToMesh)));

            if (fitPhysicsShapeToMesh && IncludeWheelsAvailable(entityType))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(so.FindProperty(nameof(includeWheels)));

                if (includeWheels)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(physicsShapeAtFloor)));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(so.FindProperty(nameof(hasWheels)));
        }

        public void DrawMonoCommonSettings(SerializedObject so, EntityType entityType)
        {
            DrawCommonSettingsForAll(so, entityType);
            EditorGUILayout.PropertyField(so.FindProperty(nameof(colliderType)));
        }

        public void DrawOffsetSettings(SerializedObject so, EntityType entityType)
        {
            if (IsCustom(entityType))
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(addOffsetCustom)), new GUIContent("Add Offset"));

                if (addOffsetCustom)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(fixPivotCustom)), new GUIContent("Fix Pivot"));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(addWheelOffsetCustom)), new GUIContent("Add Wheel Offset"));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(localOffsetCustom)), new GUIContent("Local Offset"));

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(addOffset)));

                if (addOffset)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(fixPivot)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(addWheelOffset)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(localOffset)));

                    EditorGUI.indentLevel--;
                }
            }
        }

        public void DrawPresetSettings(SerializedObject so, EntityType entityType, bool isPlayer)
        {
            if (!isPlayer)
            {
                if (IsCustom(entityType))
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(SelectedPresetCustom)), new GUIContent("Selected Preset"));
                }
                else if (IsMono(entityType))
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(SelectedPresetMono)), new GUIContent("Selected Preset"));
                }
                else
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(SelectedPreset)));
                }
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(SelectedPlayerPreset)), new GUIContent("Selected Preset"));
            }
        }

        public void DrawTemplateSettings(SerializedObject so, EntityType entityType, bool isPlayer, bool hullTemplate)
        {
            const string templateNameHullText = "Template Hull Name";
            const string templateNameText = "Template Name";

            if (isPlayer)
            {
                if (hullTemplate)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(templateNamePlayerHull)), new GUIContent(templateNameHullText));
                }

                EditorGUILayout.PropertyField(so.FindProperty(nameof(templateNamePlayer)), new GUIContent(templateNameText));
            }
            else
            {
                if (hullTemplate)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(templateNameTrafficHull)), new GUIContent(templateNameHullText));
                }

                if (IsCustom(entityType))
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(templateNameCustom)), new GUIContent(templateNameText));
                }
                else
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(templateNameSimple)), new GUIContent(templateNameText));
                }
            }
        }

        public void DrawPhysicsSettings(SerializedObject so, EntityType entityType)
        {
            if (!IsCustom(entityType))
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(Mass)));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(MassCustom)), new GUIContent("Mass"));
            }
        }

        public void DrawWheelOffsetSettings(SerializedObject so, EntityType entityType)
        {
            if (!IsCustom(entityType))
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(wheelOffset)));
            }
            else
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(wheelOffsetCustom)), new GUIContent("Wheel Offset"));
            }
        }

        public void AddCache(CarPrefabInfo carPrefabInfo)
        {
            var id = carPrefabInfo.CurrentInternalId;

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (!cachedDataDictionary.ContainsKey(id))
            {
                cachedDataDictionary.Add(id, new CachedData(carPrefabInfo));
            }
            else
            {
                cachedDataDictionary[id] = new CachedData(carPrefabInfo);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public bool IsCustom(EntityType entityType)
        {
            if (entityType == EntityType.HybridEntityCustomPhysics || entityType == EntityType.PureEntityCustomPhysics)
            {
                return true;
            }

            return false;
        }

        public bool IsMono(EntityType entityType)
        {
            if (entityType == EntityType.HybridEntityMonoPhysics)
            {
                return true;
            }

            return false;
        }

        public CachedData GetCache(CarPrefabInfo carPrefabInfo)
        {
            var cache = GetCache(carPrefabInfo.InternalID);

            if (cache == null)
            {
                cache = GetCache(carPrefabInfo.ID);
            }

            return cache;
        }

        public CachedData GetCache(string id)
        {
            if (!string.IsNullOrEmpty(id) && cachedDataDictionary.TryGetValue(id, out var cachedData))
            {
                return cachedData;
            }

            return null;
        }

        public void Merge(CacheContainer cacheContainer)
        {
            foreach (var item in cacheContainer.cachedDataDictionary)
            {
                if (!this.cachedDataDictionary.ContainsKey(item.Key))
                {
                    this.cachedDataDictionary.Add(item.Key, item.Value.GetClone());
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        [Button]
        public void Clear()
        {
            cachedDataDictionary.Clear();
            EditorSaver.SetObjectDirty(this);
        }

        private void DrawCommonSettingsForAll(SerializedObject so, EntityType entityType)
        {
            EditorGUILayout.PropertyField(so.FindProperty(nameof(scanIDSourceType)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(wheelSearchType)));
        }
    }
}