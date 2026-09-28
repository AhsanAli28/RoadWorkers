using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CreateAssetMenu(menuName = "Spirit604/Animation Baker/Character Animation Container")]
    public class CharacterAnimationContainer : ScriptableObject
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<SkinData> values = new List<SkinData>();

        public int Count => keys.Count;

        public string GetKey(int index)
        {
            return keys[index];
        }

        public void AddEntry(string key, Mesh mesh, int animationCount)
        {
            if (!keys.Contains(key))
            {
                keys.Add(key);

                var npcData = new SkinData(mesh, animationCount);

                values.Add(npcData);

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void RemoveEntry(string key)
        {
            if (HasKey(key))
            {
                var index = keys.IndexOf(key);

                keys.RemoveAt(index);
                values.RemoveAt(index);

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public void RemoveEntry(int skinIndex)
        {
            if (keys.Count > skinIndex)
            {
                keys.RemoveAt(skinIndex);
                values.RemoveAt(skinIndex);

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        // MoveArrayElement doesn't work for keys for some reason
        public void MoveEntry(int srcIndex, int newIndex)
        {
            if (srcIndex == newIndex)
            {
                return;
            }

            var oldKey = keys[srcIndex];
            var oldValue = values[srcIndex];

            keys[srcIndex] = keys[newIndex];
            values[srcIndex] = values[newIndex];

            keys[newIndex] = oldKey;
            values[newIndex] = oldValue;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void SetMaterial(int index, Material material, int lodLevel = 0)
        {
            var skinData = GetSkinData(index);

            if (skinData == null)
            {
                return;
            }

            if (skinData.SetMaterial(material, lodLevel))
            {
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public SkinData GetSkinData(int skinIndex)
        {
            if (skinIndex >= 0 && values.Count > skinIndex)
            {
                return values[skinIndex];
            }

            return null;
        }

        public SkinData GetLastSkinData()
        {
            return GetSkinData(Count - 1);
        }

        public void GetSkinData(int skinIndex, out Mesh mesh, out Material material, int lodLevel = 0)
        {
            mesh = null;
            material = null;

            if (skinIndex >= 0 && values.Count > skinIndex)
            {
                mesh = values[skinIndex].GetMesh(lodLevel);
                material = values[skinIndex].GetMaterial(lodLevel);
            }
        }

        public void AssignAnimationData(int selectedSkinIndex, int animationIndex, int animationHash, AnimationTextureData textureData, int lodLevel = 0)
        {
            var skinData = values[selectedSkinIndex];
            var animations = skinData.GetAnimations(lodLevel);

            animations[animationIndex].AnimationHash = animationHash;
            animations[animationIndex].ClipName = textureData.AnimationName;
            animations[animationIndex].ClipLength = textureData.ClipLength;
            animations[animationIndex].OriginalFrameRate = textureData.OriginalFrameRate;
            animations[animationIndex].VertexCount = textureData.VertexCount;
            animations[animationIndex].FrameRate = textureData.FrameRate;
            animations[animationIndex].FrameOffsetX = textureData.TextureOffset.x;
            animations[animationIndex].FrameOffsetY = textureData.TextureOffset.y;
            animations[animationIndex].FrameCount = textureData.FrameCount;
            animations[animationIndex].Interpolate = textureData.Interpolate;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool HasKey(string key)
        {
            return keys.Contains(key);
        }

        public bool ContainsMesh(Mesh mesh)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var skinData = values[i];

                if (skinData.ContainsMesh(mesh))
                {
                    return true;
                }
            }

            return false;
        }

        public Texture2D GetMainTexture(int index)
        {
            var skinData = GetSkinData(index);

            if (skinData != null)
            {
                return skinData.TempMainTexture;
            }

            return null;
        }

        public void SetMainTexture(int index, Texture2D mainTexture)
        {
            var skinData = GetSkinData(index);

            if (skinData != null)
            {
                skinData.TempMainTexture = mainTexture;
            }
        }

        public List<T> GetRagdollPrefabs<T>() where T : Component
        {
            List<T> prefabList = new List<T>(values.Count);

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Ragdoll)
                {
                    prefabList.Add(values[i].Ragdoll.GetComponent<T>());
                }
                else
                {
                    prefabList.Add(null);
                }
            }

            return prefabList;
        }

        public void Clear(bool recordUndo = false)
        {
#if UNITY_EDITOR
            if (recordUndo)
            {
                Undo.RegisterCompleteObjectUndo(this, "Undo character data");
            }
#endif

            keys.Clear();
            values.Clear();
        }

        public void Validate()
        {
            if (keys.Count != values.Count)
            {
                var count = keys.Count - values.Count;

                while (count > 0)
                {
                    keys.RemoveAt(keys.Count - 1);
                    count--;
                }

                EditorInternal.EditorSaver.SetObjectDirty(this);
            }
        }
    }
}