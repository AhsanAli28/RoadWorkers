using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarEngineDamageSystemSettings : SyncConfigBase
    {
        [Serializable]
        public struct CarEngineDamagedState
        {
            [Tooltip("Min vehicle health (as % of maximum health) at which engine damage starts to appear")]
            [Range(0, 1f)] public float MinHp;

            [Tooltip("Max vehicle health (as % of maximum health) at which engine damage starts to appear")]
            [Range(0, 1f)] public float MaxHp;

            [Tooltip("VFX prefab")]
            public GameObject Prefab;

            public CarEngineDamagedState(float minHp, float maxHp)
            {
                MinHp = minHp;
                MaxHp = maxHp;
                Prefab = null;
            }
        }

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html#car-engine-damage-system-settings")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [SerializeField]
        private bool engineDamageEnabled = true;

        [OnValueChanged(nameof(Sync))]
        [SerializeField]
        private List<CarEngineDamagedState> damagedStates = new List<CarEngineDamagedState>()
        {
            { new CarEngineDamagedState(0.05f, 0.2f) },
            { new CarEngineDamagedState(0.2f, 0.6f) },
            { new CarEngineDamagedState(0.6f, 1f) },
        };

        class CarEngineDamageSystemSettingsBaker : Baker<CarEngineDamageSystemSettings>
        {
            public override void Bake(CarEngineDamageSystemSettings authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                var buffer = AddBuffer<EngineStateElement>(entity);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<EngineStateSettings>();
                    var array = builder.Allocate(ref root.Settings, authoring.damagedStates.Count);

                    for (int i = 0; i < array.Length; i++)
                    {
                        var prefabEntity = GetEntity(authoring.damagedStates[i].Prefab, TransformUsageFlags.None);

                        array[i] = new EngineStateData()
                        {
                            MinHp = authoring.damagedStates[i].MinHp,
                            MaxHp = authoring.damagedStates[i].MaxHp,
                        };

                        buffer.Add(new EngineStateElement()
                        {
                            Prefab = prefabEntity
                        });
                    }

                    root.EngineDamageEnabled = authoring.engineDamageEnabled;

                    var settings = builder.CreateBlobAssetReference<EngineStateSettings>(Unity.Collections.Allocator.Persistent);

                    AddComponent(entity, new EngineStateSettingsHolder()
                    {
                        SettingsReference = settings
                    });

                    AddBlobAsset(ref settings, out var hash);
                }
            }
        }
    }
}