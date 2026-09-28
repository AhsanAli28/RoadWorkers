using Spirit604.AnimationBaker.Entities;
using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.UI;
using Spirit604.DotsCity.Simulation.Factory.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Gameplay.Services;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianAnimationTestSpawner : MonoBehaviourBase, IConfigInject
    {
        public enum PedestrianAnimationType { Idle, Walking, Running }
        public enum NpcRigType { HybridLegacy, PureGPU }

        [SerializeField] private AnimationStressView animationStressView;
        [SerializeField] private PedestrianSkinFactory pedestrianSkinFactory;
        [SerializeField] private PedestrianCrowdSkinFactory pedestrianGPUSkinFactory;
        [SerializeField] private EntityWorldService entityWorldService;
        [SerializeField] private SceneService sceneService;
        [SerializeField] private FPSDisplay fpsDisplay;
        [SerializeField] private Transform spawnPoint;

        [Expandable]
        [SerializeField] private PedestrianTestAnimationSpawnerConfig pedestrianTestAnimationSpawnerConfig;

        private CrowdSkinProviderSystem crowdSkinProviderSystem;
        private List<GameObject> hybridEntities = new List<GameObject>();
        private NativeList<Entity> pureEntities;
        private NpcRigType currentRigType;
        private int currentCount;
        private int sliderCount;
        private bool spawned;
        private bool isInitialized;

        private int SpawnCount
        {
            get
            {
                var spawnCount = 0;

                switch (pedestrianTestAnimationSpawnerConfig.NpcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        spawnCount = pedestrianTestAnimationSpawnerConfig.SpawnCount;
                        break;
                    case NpcRigType.PureGPU:
                        spawnCount = pedestrianTestAnimationSpawnerConfig.SpawnCountGPU;
                        break;
                }

                return spawnCount;
            }
        }

        private int MaxCount
        {
            get
            {
                var spawnCount = 0;

                switch (pedestrianTestAnimationSpawnerConfig.NpcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        spawnCount = pedestrianTestAnimationSpawnerConfig.SpawnCountMaxHybrid;
                        break;
                    case NpcRigType.PureGPU:
                        spawnCount = pedestrianTestAnimationSpawnerConfig.SpawnCountMaxGPU;
                        break;
                }

                return spawnCount;
            }
        }

        private int SkinCount
        {
            get
            {
                switch (pedestrianTestAnimationSpawnerConfig.NpcRigType)
                {
                    case NpcRigType.HybridLegacy:
                        return pedestrianSkinFactory.SkinCount;
                    case NpcRigType.PureGPU:
                        return pedestrianGPUSkinFactory.SkinCount;
                }

                return 0;
            }
        }

        private void Start()
        {
            Initialize();
            InitialSpawn();

            sceneService.Construct(entityWorldService);

            animationStressView.OnNpcTypeChanged += AnimationStressView_OnNpcTypeChanged;
            animationStressView.OnCountChanged += AnimationStressView_OnCountChanged;
            animationStressView.OnRandomizeChanged += AnimationStressView_OnRandomizeChanged;
            animationStressView.OnUpdateClick += AnimationStressView_OnUpdateClick;
            animationStressView.OnExitClick += AnimationStressView_OnExitClick;
        }

        private void OnDestroy()
        {
            CleanScene();

            if (pureEntities.IsCreated)
            {
                pureEntities.Dispose();
            }
        }

        public void InjectConfig(object config)
        {
            var newConfig = config as PedestrianTestAnimationSpawnerConfig;

            if (newConfig && pedestrianTestAnimationSpawnerConfig != newConfig)
            {
                Initialize();
                pedestrianTestAnimationSpawnerConfig = newConfig;
                InitialSpawn(true);
            }
        }

        private void InitialSpawn(bool force = false)
        {
            if (!force && spawned)
            {
                return;
            }

            spawned = true;
            Spawn();
            animationStressView.Initialize(currentRigType);
            animationStressView.Initialize(pedestrianTestAnimationSpawnerConfig.RandomizeSkin);
        }

        private void Spawn(int startIndex, int spawnCount)
        {
            var animationIndex = (int)pedestrianTestAnimationSpawnerConfig.AnimationType;
            int objectIndexInRow = 0;
            var skinCount = SkinCount;
            currentRigType = pedestrianTestAnimationSpawnerConfig.NpcRigType;
            currentCount += spawnCount;
            sliderCount = currentCount;

            animationStressView.Initialize(currentCount, MaxCount);

            int spawned = 0;
            int index = startIndex;

            while (spawned < spawnCount)
            {
                int skinIndex = 0;

                if (pedestrianTestAnimationSpawnerConfig.RandomizeSkin)
                {
                    skinIndex = UnityEngine.Random.Range(0, skinCount);
                }

                Vector3 sourceSpawnPoint = spawnPoint != null ? spawnPoint.transform.position : Vector3.zero;

                float x = sourceSpawnPoint.x + (objectIndexInRow - (float)pedestrianTestAnimationSpawnerConfig.ObjectInRowCount / 2) * pedestrianTestAnimationSpawnerConfig.SpawnOffset;

                int rowNumber = Mathf.FloorToInt((float)index / pedestrianTestAnimationSpawnerConfig.ObjectInRowCount);
                float z = sourceSpawnPoint.z - rowNumber * pedestrianTestAnimationSpawnerConfig.SpawnOffset;
                Vector3 spawnPosition = new Vector3(x, 0, z);

                switch (currentRigType)
                {
                    case NpcRigType.HybridLegacy:
                        {
                            var prefab = pedestrianSkinFactory.GetEntry(skinIndex).Skin;
                            var instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
                            hybridEntities.Add(instance);

                            var animator = instance.GetComponent<Animator>();

                            float walkParam = 0;

                            switch (animationIndex)
                            {
                                case 1:
                                    {
                                        walkParam = AnimatorConstans.ANIMATOR_WALK_PARAMETER;
                                        break;
                                    }
                                case 2:
                                    {
                                        walkParam = 1f;
                                        break;
                                    }
                            }

                            animator.SetFloat(AnimatorConstans.ANIMATOR_MOVEMENT_KEY, walkParam);
                            break;
                        }
                    case NpcRigType.PureGPU:
                        {
                            var EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                            var entity = EntityManager.CreateEntity();

                            if (!pureEntities.IsCreated)
                            {
                                pureEntities = new NativeList<Entity>(Allocator.Persistent);
                            }

                            pureEntities.Add(entity);

                            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

                            var animationHash = pedestrianGPUSkinFactory.GetSavedAnimationHash(skinIndex, animationIndex);

                            commandBuffer.AddComponent(entity, new SkinUpdateComponent()
                            {
                                NewAnimationHash = animationHash
                            });

                            commandBuffer.AddComponent(entity, new SkinAnimatorData()
                            {
                                SkinIndex = skinIndex,
                            });

                            commandBuffer.AddComponent(entity, typeof(GPUSkinTag));
                            commandBuffer.AddComponent(entity, typeof(UpdateSkinTag));

                            commandBuffer.AddComponent(entity, typeof(AnimationTransitionData));

                            ShaderUtils.AddShaderComponents(ref commandBuffer, entity, true);

                            commandBuffer.AddComponent(entity, new WorldToLocal_Tag());
                            commandBuffer.AddComponent(entity, new PerInstanceCullingTag());
                            commandBuffer.AddComponent(entity, new BlendProbeTag());
                            commandBuffer.AddComponent(entity, new HasSkinTag());
                            commandBuffer.AddComponent(entity, new DisableUnloadSkinTag());

                            commandBuffer.AddComponent(entity, LocalTransform.FromPosition(spawnPosition));

                            commandBuffer.AddComponent(entity, new LocalToWorld());
                            commandBuffer.AddComponent(entity, new MaterialMeshInfo());

                            Mesh mesh;
                            Material material;

                            pedestrianGPUSkinFactory.GetSkinData(skinIndex, animationIndex, out mesh, out material);

                            commandBuffer.AddComponent(entity, new RenderBounds()
                            {
                                Value = mesh.bounds.ToAABB()
                            });

                            commandBuffer.AddSharedComponentManaged(entity, RenderFilterSettings.Default);

                            var renderMeshArray = crowdSkinProviderSystem.TotalRenderMeshData;
                            commandBuffer.AddSharedComponentManaged(entity, renderMeshArray);

                            commandBuffer.Playback(EntityManager);
                            commandBuffer.Dispose();

                            break;
                        }
                }

                objectIndexInRow = ++objectIndexInRow % pedestrianTestAnimationSpawnerConfig.ObjectInRowCount;

                spawned++;
                index++;
            }

            LaunchWaitForSpawn();
        }

        private void CleanScene(int cleanCount)
        {
            switch (currentRigType)
            {
                case NpcRigType.HybridLegacy:
                    {
                        while (cleanCount >= 0 && hybridEntities.Count > 0)
                        {
                            int lastIndex = hybridEntities.Count - 1;

                            if (hybridEntities[lastIndex] != null)
                            {
                                Destroy(hybridEntities[lastIndex].gameObject);
                            }

                            hybridEntities.RemoveAt(lastIndex);

                            cleanCount--;
                        }

                        currentCount = hybridEntities.Count;
                        break;
                    }
                case NpcRigType.PureGPU:
                    {
                        var world = World.DefaultGameObjectInjectionWorld;

                        if (world == null)
                        {
                            return;
                        }

                        var entityManager = world.EntityManager;

                        while (cleanCount >= 0 && pureEntities.IsCreated && pureEntities.Length > 0)
                        {
                            int lastIndex = pureEntities.Length - 1;

                            if (entityManager.Exists(pureEntities[lastIndex]))
                            {
                                entityManager.DestroyEntity(pureEntities[lastIndex]);
                            }

                            pureEntities.RemoveAt(lastIndex);
                            cleanCount--;
                        }

                        currentCount = pureEntities.IsCreated ? pureEntities.Length : 0;
                        break;
                    }
            }
        }

        private void CleanScene()
        {
            switch (currentRigType)
            {
                case NpcRigType.HybridLegacy:
                    {
                        CleanScene(hybridEntities.Count);

                        break;
                    }
                case NpcRigType.PureGPU:
                    {
                        CleanScene(pureEntities.Length);
                        break;
                    }
            }
        }

        private void Respawn()
        {
            CleanScene();
            Spawn();
        }

        private void Spawn()
        {
            currentCount = 0;
            Spawn(0, SpawnCount);
        }

        private void LaunchWaitForSpawn()
        {
            if (fpsDisplay != null)
            {
                StartCoroutine(WaitForSpawn());
            }
        }

        private IEnumerator WaitForSpawn()
        {
            yield return new WaitWhile(() =>
            {
                return hybridEntities.Count == 0 && pureEntities.Length == 0;
            });

            const float delay = 0.2f;
            fpsDisplay.ResetWithDelay(delay, true);
        }

        private void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<LoadPermamentGPUSkinSystem, EarlyEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<UpdatePermamentGPUSkinSystem, LateEventGroup>();

            pedestrianGPUSkinFactory.CreateFactory();

            DefaultWorldUtils.CreateAndAddSystemUnmanaged<GPUAnimatorSystem, LateEventGroup>();

            crowdSkinProviderSystem = DefaultWorldUtils.CreateAndAddSystemManaged<CrowdSkinProviderSystem, InitializationSystemGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<InitGPUSkinSystem, MainThreadEventGroup>();

            crowdSkinProviderSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<CrowdSkinProviderSystem>();
            crowdSkinProviderSystem.Initialize(pedestrianGPUSkinFactory);
            crowdSkinProviderSystem.CreateBlobEntity();
        }

        private void AnimationStressView_OnNpcTypeChanged(PedestrianAnimationTestSpawner.NpcRigType npcRigType)
        {
            if (currentRigType == npcRigType)
            {
                return;
            }

            CleanScene();
            currentRigType = npcRigType;
            pedestrianTestAnimationSpawnerConfig.NpcRigType = npcRigType;
            Spawn();
        }

        private void AnimationStressView_OnCountChanged(int newCount)
        {
            this.sliderCount = newCount;
            animationStressView.SetInteractableState(true);
        }

        private void AnimationStressView_OnRandomizeChanged(bool randomizeSkin)
        {
            pedestrianTestAnimationSpawnerConfig.RandomizeSkin = randomizeSkin;
            Respawn();
        }

        private void AnimationStressView_OnUpdateClick()
        {
            animationStressView.SetInteractableState(false);
            var diff = sliderCount - currentCount;

            if (diff < 0)
            {
                CleanScene(Mathf.Abs(diff));
            }
            else if (diff > 0)
            {
                Spawn(currentCount, diff);
            }
        }

        private void AnimationStressView_OnExitClick()
        {
            animationStressView.SwitchCanvasState(false);
            sceneService.LoadScene(0);
        }
    }
}