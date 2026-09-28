using Spirit604.Attributes;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming.Authoring
{
    [TemporaryBakingType]
    public struct SectionObjectBakingData : IComponentData
    {
        public SectionObjectType SectionObjectType;
        public Entity RelatedObject;
        public float3 Position;
        public NativeArray<Entity> ChildEntities;
    }

    [DisallowMultipleComponent]
    public class SectionObjectAuthoring : MonoBehaviourBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#section-object-authoring")]
        private string link;

        [Tooltip("" +
            "<b>Attach To Closest</b> : attach to nearest road section\r\n\r\n" +
            "<b>Create New If Nessesary</b> : create a new road section if doesn't exist with the currently computed section hash\r\n\r\n" +
            "<b>Provider Object</b> : object has a component that implements the `IProviderObject` interface, that provides a reference to the associated object section\r\n\r\n" +
            "<b>Custom Object</b> : user's own associated object section")]
        [SerializeField] private SectionObjectType sectionObjectType;

        [SerializeField] private bool includeChilds = true;

        [ShowIf(nameof(HasCustomObject))]
        [SerializeField] private GameObject customSectionObject;

        public IRelatedObjectProvider ObjectProvider { get; private set; }

        private bool HasCustomObject => sectionObjectType == SectionObjectType.CustomObject;

        class SectionObjectAuthoringBaker : Baker<SectionObjectAuthoring>
        {
            private NativeList<Entity> childEntities;

            public override void Bake(SectionObjectAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                childEntities = new NativeList<Entity>(Allocator.TempJob);

                if (authoring.includeChilds)
                {
                    AddChilds(authoring.transform);
                }
                else
                {
                    var parentEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                    childEntities.Add(parentEntity);
                }

                Entity relatedEntity = Entity.Null;

                switch (authoring.sectionObjectType)
                {
                    case SectionObjectType.ProviderObject:
                        {
                            relatedEntity = GetProviderEntity(authoring);
                            break;
                        }
                    case SectionObjectType.CustomObject:
                        {
                            if (authoring.customSectionObject)
                            {
                                relatedEntity = GetEntity(authoring.customSectionObject, TransformUsageFlags.Dynamic);
                            }

                            break;
                        }
                }

                AddComponent(entity, new SectionObjectBakingData()
                {
                    SectionObjectType = authoring.sectionObjectType,
                    RelatedObject = relatedEntity,
                    Position = authoring.transform.position,
                    ChildEntities = childEntities.ToArray(Allocator.Temp)
                });

                childEntities.Dispose();
            }

            private Entity GetProviderEntity(SectionObjectAuthoring authoring)
            {
                if (authoring.ObjectProvider == null)
                {
                    authoring.ObjectProvider = authoring.GetComponent<IRelatedObjectProvider>();
                }

                var objectProvider = authoring.ObjectProvider;

                if (objectProvider != null && objectProvider.RelatedObject)
                {
                    if (objectProvider.RelatedObject)
                    {
                        var relatedEntity = GetEntity(objectProvider.RelatedObject, TransformUsageFlags.Dynamic);

                        return relatedEntity;
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"SectionObjectAuthoring. Object '{authoring.name}' doesn't have a component that implements 'IRelatedObjectProvider', add implementation to any component and assign related scene section object");
                }

                return Entity.Null;
            }

            private void AddChilds(Transform sourceTransform)
            {
                var entity = GetEntity(sourceTransform.gameObject, TransformUsageFlags.Dynamic);

                childEntities.Add(entity);

                for (int i = 0; i < sourceTransform.childCount; i++)
                {
                    AddChilds(sourceTransform.GetChild(i));
                }
            }
        }
    }
}