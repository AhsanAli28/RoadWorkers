using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarEntityAuthoringBase : MonoBehaviourBase, ICarIDProvider
    {
        public enum BoundsSourceType { EntityPhysicsShape, NavMeshObstacle, HullMesh, Custom }

        [OnValueChanged(nameof(OnIDChanged))]
        [HideIf(nameof(hybridMono))]
        [SerializeField] private bool customID;

        [ShowIf(nameof(customID))]
        [SerializeField] private string id;

        [HideIf(nameof(hybridMono))]
        [SerializeField] private MeshRenderer hullMeshRenderer;

        [HideIf(nameof(hybridMono))]
        [SerializeField] private PhysicsShapeAuthoring physicsShape;

        [HideInInspector]
        [SerializeField] private bool hybridMono;

        [Tooltip("Enum type to solve the issue of friendly fire in damage systems")]
        [SerializeField] private FactionType factionType;

        [Tooltip("Enum type to solve the issue of friendly fire in damage systems")]
        [SerializeField] private CarType carType;

        [Tooltip("Selected bounds source for the entity bounds")]
        [HideIf(nameof(hybridMono))]
        [SerializeField] private BoundsSourceType boundsSourceType;

#if UNITY_EDITOR
        [SerializeField] private bool showBounds = true;

        [ShowIf(nameof(showBounds))]
#endif

        [SerializeField] private Color boundsColor = Color.white;

        [ShowIf(nameof(CustomBounds))]
        [SerializeField] private Vector3 boundsSize = new Vector3(2f, 2f, 4f);

        [ShowIf(nameof(CustomBounds))]
        [SerializeField] private Vector3 boundsCenter = new Vector3(0, 0.7f, 0);

        public MeshRenderer HullMeshRenderer { get => hullMeshRenderer; set => hullMeshRenderer = value; }

        public PhysicsShapeAuthoring PhysicsShape { get => physicsShape; set => physicsShape = value; }

        public FactionType FactionType { get => factionType; set => factionType = value; }

        public CarType CarType { get => carType; set => carType = value; }

        public virtual string ID
        {
            get
            {
                if (customID)
                {
                    return id;
                }

                return MeshId;
            }
            set
            {
                if (id != value)
                {
                    id = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        private string MeshId => hullMeshRenderer != null ? hullMeshRenderer.GetComponent<MeshFilter>()?.sharedMesh?.name : string.Empty;

        private bool CustomBounds => CurrentBoundsSourceType == BoundsSourceType.Custom;

        public virtual bool CustomID { get => customID; set => customID = value; }

        public bool HybridMono { get => hybridMono; set => hybridMono = value; }

        public BoundsSourceType CurrentBoundsSourceType
        {
            get
            {
                if (hybridMono)
                    return BoundsSourceType.Custom;

                return boundsSourceType;
            }
        }

        public class CarEntityAuthoringBaseBaker : Baker<CarEntityAuthoringBase>
        {
            public override void Bake(CarEntityAuthoringBase authoring)
            {
                Bake(this, GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic), authoring);
            }

            public static void Bake(IBaker baker, Entity entity, CarEntityAuthoringBase authoring)
            {
                baker.AddComponent(entity, typeof(CarTag));
                baker.AddComponent(entity, typeof(AliveTag));
                baker.AddComponent(entity, typeof(ObstacleTag));
                baker.AddComponent(entity, typeof(VelocityComponent));
                baker.AddComponent(entity, new FactionTypeComponent { Value = authoring.factionType });
                baker.AddComponent(entity, new CarTypeComponent { CarType = authoring.carType });
                baker.AddComponent(entity, CullComponentsExtension.GetComponentSet());
                PoolEntityUtils.AddPoolComponents(baker, entity);

                baker.AddComponent<CarModelComponent>(entity);

                if (!authoring.HybridMono)
                {
                    var meshEntity = Entity.Null;

                    if (authoring.hullMeshRenderer)
                    {
                        meshEntity = baker.GetEntity(authoring.hullMeshRenderer.gameObject, TransformUsageFlags.Dynamic);
                    }

                    baker.AddComponent(entity, new CarRelatedHullComponent { HullEntity = meshEntity });
                }

                var bounds = authoring.GetBounds();

                baker.AddComponent(entity, new BoundsComponent
                {
                    Extents = bounds.extents,
                    Center = bounds.center,
                });

                bool hasIgnition = true;

                if (hasIgnition)
                {
                    baker.AddComponent(entity, new CarIgnitionData
                    {
                    });
                }
            }
        }

        public void SetCustomBounds(Bounds bounds)
        {
            boundsSize = bounds.size;
            boundsCenter = bounds.center;
            boundsSourceType = BoundsSourceType.Custom;
            EditorSaver.SetObjectDirty(this);
        }

        protected Bounds GetBounds()
        {
            switch (CurrentBoundsSourceType)
            {
                case BoundsSourceType.EntityPhysicsShape:
                    if (physicsShape)
                    {
                        var boxGeometry = physicsShape.GetBoxProperties();

                        float3 size = boxGeometry.Size;
                        float3 center = boxGeometry.Center;

                        return new Bounds(center, size);
                    }
                    break;
                case BoundsSourceType.NavMeshObstacle:

                    break;
                case BoundsSourceType.HullMesh:
                    if (hullMeshRenderer)
                    {
                        return new Bounds(hullMeshRenderer.bounds.center - transform.position, hullMeshRenderer.bounds.size);
                    }
                    break;
                case BoundsSourceType.Custom:
                    return new Bounds(boundsCenter, boundsSize);
            }

            return default;
        }

        private void OnIDChanged()
        {
            if (customID && string.IsNullOrEmpty(id) && hullMeshRenderer)
            {
                ID = MeshId;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!showBounds)
                return;

            if (CurrentBoundsSourceType == BoundsSourceType.HullMesh || CustomBounds)
            {
                var bounds = GetBounds();
                var origin = transform.position + bounds.center;
                Gizmos.color = boundsColor;
                Gizmos.DrawWireCube(origin, bounds.size);
            }
        }
#endif
    }
}