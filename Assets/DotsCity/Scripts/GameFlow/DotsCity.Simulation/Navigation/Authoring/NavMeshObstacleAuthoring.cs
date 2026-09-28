using Spirit604.Attributes;
using Spirit604.Extensions;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.DotsCity.NavMesh.Authoring
{
    public class NavMeshObstacleAuthoring : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCar.html#navmeshobstacleauthoring")]
        [SerializeField] private string link;

        [SerializeField] private bool showBounds;
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 size = Vector3.one;
        [SerializeField] private bool carve;

        [ShowIf(nameof(carve))]
        [SerializeField] private float moveThreshold = 0.1f;

        [ShowIf(nameof(carve))]
        [SerializeField] private float timeToStationary = 0.5f;

        [ShowIf(nameof(carve))]
        [SerializeField] private bool carveOnlyStationary;

        public bool Carve { get => carve; set => carve = value; }
        public float MoveThreshold { get => moveThreshold; set => moveThreshold = value; }
        public float TimeToStationary { get => timeToStationary; set => timeToStationary = value; }
        public bool CarveOnlyStationary { get => carveOnlyStationary; set => carveOnlyStationary = value; }

        public Bounds Bounds
        {
            get
            {
                return new Bounds(center, size);
            }
            set
            {
                center = value.center;
                size = value.size;
            }
        }

        class NavMeshObstacleAuthoringBaker : Baker<NavMeshObstacleAuthoring>
        {
            public override void Bake(NavMeshObstacleAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new NavMeshObstacleData()
                {
                    Center = authoring.center,
                    Size = authoring.size,
                    Carve = authoring.carve,
                    MoveThreshold = authoring.moveThreshold,
                    TimeToStationary = authoring.timeToStationary,
                    CarveOnlyStationary = authoring.carveOnlyStationary,
                });

                AddComponent(entity, new NavMeshObstacleLoadTag());
            }
        }

        [Button]
        public void FitToBounds()
        {
            var meshes = GetComponentsInChildren<MeshRenderer>();

            if (meshes?.Length > 0)
            {
                var bounds = meshes[0].bounds;

                for (int i = 1; i < meshes.Length; i++)
                {
                    bounds.Encapsulate(meshes[i].bounds);
                }

                center = bounds.center - transform.position;
                size = bounds.size;

                var navMeshObtacle = GetComponent<NavMeshObstacle>();

                if (navMeshObtacle)
                {
                    DestroyImmediate(navMeshObtacle);
                }

                EditorSaver.SetObjectDirty(this);
            }
        }

        private void Reset()
        {
            FitToBounds();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showBounds)
            {
                return;
            }

            UnityMathematicsExtension.DrawGizmosRotatedCube(transform.position, transform.rotation, new Bounds()
            {
                size = size,
                center = center
            }
            , Color.white);
        }
#endif
    }
}