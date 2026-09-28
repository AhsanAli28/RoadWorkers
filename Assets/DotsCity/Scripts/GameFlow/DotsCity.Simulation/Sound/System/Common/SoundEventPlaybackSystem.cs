using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Simulation.Sound.Utils;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    public partial class SoundEventPlaybackSystem : BeginSimulationSystemBase
    {
        #region Helper types

        public struct SoundEventData
        {
            public Entity TargetEntity;
            public int Id;
            public float3 Position;
            public float Volume;
            public float Delay;
            public SoundType SoundType;

            public SoundEventData(int id, float3 position) : this()
            {
                TargetEntity = Entity.Null;
                Id = id;
                Position = position;
                Volume = 1f;
                SoundType = SoundType.Default;
            }

            public SoundEventData(int id, float3 position, float volume, float delay = 0)
            {
                TargetEntity = Entity.Null;
                Id = id;
                Position = position;
                Volume = volume;
                Delay = delay;
                SoundType = SoundType.Default;
            }

            public SoundEventData(int id, float3 position, float volume, SoundType soundType, float delay = 0) : this(id, position, volume, delay)
            {
                SoundType = soundType;
            }

            public SoundEventData(Entity targetEntity, int id, float3 position, float volume, SoundType soundType, float delay = 0) : this(id, position, volume, delay)
            {
                TargetEntity = targetEntity;
            }
        }

        public struct Singleton : IComponentData
        {
            public UnsafeQueue<SoundEventData> EventQueue;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PlayOneShot(int id, float3 position, float volume = 1f)
            {
                if (id == -1) return;
                EventQueue.Enqueue(new SoundEventData(id, position, volume, SoundType.OneShot));
            }
        }

        private EntityQuery soundPrefabQuery;
        private UnsafeQueue<SoundEventData> eventQueue;
        private ISoundPlayer soundPlayer;

        #endregion

        protected override void OnCreate()
        {
            base.OnCreate();
            soundPrefabQuery = SoundExtension.GetSoundQuery(EntityManager);
            eventQueue = new UnsafeQueue<SoundEventData>(Allocator.Persistent);

            EntityManager.AddComponentData(SystemHandle, new Singleton()
            {
                EventQueue = eventQueue
            });

            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            eventQueue.Dispose();
        }

        protected override void OnUpdate()
        {
            if (eventQueue.IsEmpty()) return;

            bool isCreated = false;

            EntityCommandBuffer commandBuffer = default;

            var soundPrefabEntity = soundPrefabQuery.GetSingletonEntity();

            while (eventQueue.Count > 0)
            {
                var soundEventData = eventQueue.Dequeue();

                switch (soundEventData.SoundType)
                {
                    case SoundType.OneShot:
                        {
                            soundPlayer.PlayOneShot(soundEventData.Id, soundEventData.Position, soundEventData.Volume);
                            break;
                        }
                    default:
                        {
                            if (!isCreated)
                            {
                                isCreated = true;
                                commandBuffer = GetCommandBuffer();
                            }

                            commandBuffer.CreateSoundEntity(soundPrefabEntity, soundEventData.Id, soundEventData.Position, soundEventData.Volume);
                            break;
                        }
                }
            }

            if (isCreated)
            {
                AddCommandBufferForProducer();
            }
        }

        public void Initialize(ISoundPlayer soundPlayer)
        {
            this.soundPlayer = soundPlayer;
            Enabled = true;
        }
    }
}