using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Sound.Pedestrian
{
    [UpdateInGroup(typeof(BeginSimulationGroup), OrderLast = true)]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CrowdSoundSystem : ISystem
    {
        private EntityQuery playerQuery;
        private NativeList<int> keys;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            keys = new NativeList<int>(9, Allocator.Persistent);

            playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerTag, LocalTransform>()
                .Build();

            state.RequireForUpdate(playerQuery);
            state.RequireForUpdate<NpcHashMapSystem.Singleton>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (keys.IsCreated)
            {
                keys.Dispose();
            }
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            if (playerQuery.CalculateEntityCount() != 1)
                return;

            var crowdSoundJob = new CrowdSoundJob()
            {
                HashKeys = keys,
                NpcHashMapSingleton = SystemAPI.GetSingleton<NpcHashMapSystem.Singleton>(),
                PlayerPosition = playerQuery.GetSingleton<LocalTransform>().Position,
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            crowdSoundJob.Run();
        }

        [BurstCompile]
        public partial struct CrowdSoundJob : IJobEntity
        {
            public NativeList<int> HashKeys;

            [ReadOnly]
            public NpcHashMapSystem.Singleton NpcHashMapSingleton;

            [ReadOnly]
            public float3 PlayerPosition;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref CrowdSoundVolume crowdSoundVolume,
                ref SoundVolume soundVolume,
                in CrowdSoundData crowdSoundData)
            {
                int innerNpcCount = 0;

                HashKeys.Clear();

                var playerPositionFlat = PlayerPosition.Flat();

                HashMapHelper.GetHashMapPosition9Cells(ref HashKeys, playerPositionFlat, offset: crowdSoundData.InnerCellOffset);

                for (int i = 0; i < HashKeys.Length; i++)
                {
                    var key = HashKeys[i];
                    var count = NpcHashMapSingleton.NpcMultiHashMap.CountValuesForKey(key);
                    innerNpcCount += count;
                }

                HashKeys.Clear();
                HashMapHelper.GetHashMapPosition9Cells(ref HashKeys, playerPositionFlat, offset: crowdSoundData.OuterCellOffset);

                int outerNpcCount = 0;

                for (int i = 0; i < HashKeys.Length; i++)
                {
                    var key = HashKeys[i];
                    var count = NpcHashMapSingleton.NpcMultiHashMap.CountValuesForKey(key);
                    outerNpcCount += count;
                }

                var minCurrentVolume = innerNpcCount + outerNpcCount >= crowdSoundData.MinCrowdSoundCount ? crowdSoundData.MinVolume : 0;

                var currentVolume1 = (float)innerNpcCount / crowdSoundData.InnerCrowdSoundCount;
                var currentVolume2 = (float)outerNpcCount / crowdSoundData.OuterCrowdSoundCount;

                currentVolume2 = math.clamp(currentVolume2, minCurrentVolume, crowdSoundData.OuterMaxVolume);
                currentVolume2 = currentVolume2 * (crowdSoundData.MaxVolume - currentVolume1);

                var currentVolume = currentVolume1 + currentVolume2;
                currentVolume = math.clamp(currentVolume, minCurrentVolume, crowdSoundData.MaxVolume);

                var yPos = math.abs(PlayerPosition.y);

                if (yPos >= crowdSoundData.MinHeightMuting)
                {
                    float muteRate = 0;

                    if (yPos < crowdSoundData.MaxHeight)
                    {
                        muteRate = 1 - math.unlerp(crowdSoundData.MinHeightMuting, crowdSoundData.MaxHeight, yPos);
                    }

                    currentVolume = currentVolume * muteRate;
                }

                crowdSoundVolume.TargetVolume = currentVolume;
                crowdSoundVolume.CurrentVolume = math.lerp(crowdSoundVolume.CurrentVolume, crowdSoundVolume.TargetVolume, crowdSoundData.LerpVolumeSpeed * DeltaTime);

                soundVolume.Volume = crowdSoundVolume.CurrentVolume;
            }
        }
    }
}
