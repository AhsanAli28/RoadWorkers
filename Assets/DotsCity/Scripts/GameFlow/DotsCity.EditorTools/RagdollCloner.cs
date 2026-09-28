#if UNITY_EDITOR
using Spirit604.CityEditor;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.EditorTools
{
    public class RagdollCloner : EditorWindowBase
    {
        public class RadgollPart
        {
            public Transform SourceTransform;
            public CapsuleCollider CapsuleCollider;
            public BoxCollider BoxCollider;
            public SphereCollider SphereCollider;
            public Rigidbody Rigidbody;
            public CharacterJoint CharacterJoint;
            public List<int> SiblingIndexes = new List<int>();
            public int Layer;
        }

        [SerializeField]
        private GameObject sourceCharacter;

        [SerializeField]
        private List<GameObject> targetCharacters = new List<GameObject>();

        private List<GameObject> targetCharacterPrefabs = new List<GameObject>();

        private List<RadgollPart> ragdollBinding = new List<RadgollPart>();
        private Dictionary<Transform, RadgollPart> transformToRagdollBinding = new Dictionary<Transform, RadgollPart>();
        private bool created;

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Ragdoll Cloner")]
        public static RagdollCloner ShowWindow()
        {
            RagdollCloner window = (RagdollCloner)GetWindow(typeof(RagdollCloner));
            window.titleContent = new GUIContent("Ragdoll Cloner");
            return window;
        }

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(sourceCharacter)));
            EditorGUILayout.PropertyField(so.FindProperty(nameof(targetCharacters)));

            so.ApplyModifiedProperties();

            if (GUILayout.Button("Clone"))
            {
                Create();
            }

            if (created)
            {
                EditorGUILayout.HelpBox("All ragdolls successfully cloned", MessageType.Info);
            }
        }

        public void Create()
        {
            created = false;

            if (sourceCharacter == null)
            {
                return;
            }

            ragdollBinding.Clear();
            transformToRagdollBinding.Clear();
            targetCharacterPrefabs.Clear();

            var colls = sourceCharacter.GetComponentsInChildren<Collider>();

            foreach (var col in colls)
            {
                var ragdollPart = new RadgollPart()
                {
                    SourceTransform = col.transform,
                    CapsuleCollider = col.GetComponent<CapsuleCollider>(),
                    BoxCollider = col.GetComponent<BoxCollider>(),
                    SphereCollider = col.GetComponent<SphereCollider>(),
                    Rigidbody = col.GetComponent<Rigidbody>(),
                    CharacterJoint = col.GetComponent<CharacterJoint>(),
                    SiblingIndexes = GetTransformIndexes(sourceCharacter.transform, col.transform),
                    Layer = col.gameObject.layer,
                };

                ragdollBinding.Add(ragdollPart);
                transformToRagdollBinding.Add(col.transform, ragdollPart);
            }

            foreach (var targetCharacter in targetCharacters)
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(targetCharacter);

                targetCharacterPrefabs.TryToAdd(prefab);
            }

            ClearTargets();

            foreach (var targetCharacter in targetCharacterPrefabs)
            {
                foreach (var binding in ragdollBinding)
                {
                    var boneTransform = GetTransform(targetCharacter, binding.SiblingIndexes);

                    if (boneTransform)
                    {
                        if (binding.CapsuleCollider)
                        {
                            var capsule = boneTransform.gameObject.AddComponent<CapsuleCollider>();
                            EditorUtility.CopySerialized(binding.CapsuleCollider, capsule);
                        }

                        if (binding.BoxCollider)
                        {
                            var boxCollider = boneTransform.gameObject.AddComponent<BoxCollider>();
                            EditorUtility.CopySerialized(binding.BoxCollider, boxCollider);
                        }

                        if (binding.SphereCollider)
                        {
                            var sphereCollider = boneTransform.gameObject.AddComponent<SphereCollider>();
                            EditorUtility.CopySerialized(binding.SphereCollider, sphereCollider);
                        }

                        if (binding.Rigidbody)
                        {
                            var rigidbody = boneTransform.gameObject.AddComponent<Rigidbody>();
                            EditorUtility.CopySerialized(binding.Rigidbody, rigidbody);
                        }

                        if (binding.CharacterJoint)
                        {
                            var characterJoint = boneTransform.gameObject.AddComponent<CharacterJoint>();
                            EditorUtility.CopySerialized(binding.CharacterJoint, characterJoint);

                            var indexes = transformToRagdollBinding[binding.CharacterJoint.connectedBody.transform].SiblingIndexes;

                            Transform connectedTransform = GetTransform(targetCharacter, indexes);

                            characterJoint.connectedBody = connectedTransform.GetComponent<Rigidbody>();
                            EditorSaver.SetObjectDirty(characterJoint);
                        }

                        boneTransform.gameObject.layer = binding.Layer;
                    }
                }

                PrefabUtility.SavePrefabAsset(targetCharacter);
            }

            created = true;
        }

        private void ClearTargets()
        {
            foreach (var targetCharacter in targetCharacterPrefabs)
            {
                var path = AssetDatabase.GetAssetPath(targetCharacter);

                var prefabRoot = PrefabUtility.LoadPrefabContents(path);

                var joints = prefabRoot.GetComponentsInChildren<CharacterJoint>().ToArray();

                for (int i = 0; i < joints?.Length; i++)
                {
                    DestroyImmediate(joints[i]);
                }

                var cols = prefabRoot.GetComponentsInChildren<Collider>().ToArray();

                for (int i = 0; i < cols?.Length; i++)
                {
                    DestroyImmediate(cols[i]);
                }

                var rbs = prefabRoot.GetComponentsInChildren<Rigidbody>().ToArray();

                for (int i = 0; i < rbs?.Length; i++)
                {
                    DestroyImmediate(rbs[i]);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private Transform GetTransform(GameObject sourceObject, List<int> indexes)
        {
            Transform targetTransform = sourceObject.transform;

            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                targetTransform = targetTransform.GetChild(indexes[i]);
            }

            return targetTransform;
        }

        private List<int> GetTransformIndexes(Transform parent, Transform targetTranform)
        {
            var siblingIndexes = new List<int>();

            while (targetTranform != parent)
            {
                siblingIndexes.Add(targetTranform.GetSiblingIndex());
                targetTranform = targetTranform.parent;
            }

            return siblingIndexes;
        }
    }
}
#endif
