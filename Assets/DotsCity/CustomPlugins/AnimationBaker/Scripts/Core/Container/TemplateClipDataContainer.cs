using Spirit604.AnimationBaker.EditorInternal;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [CreateAssetMenu(menuName = Constans.AssetRootPath + "Template Clip Data")]
    public class TemplateClipDataContainer : ScriptableObject
    {
        [SerializeField]
        private List<ClipData> clips = new List<ClipData>();

        public List<ClipData> GetClips() => CloneClips(clips);

        public void SaveClips(List<ClipData> newClips, bool saveCopy = false, bool recordUndo = false)
        {
#if UNITY_EDITOR
            if (recordUndo)
            {
                Undo.RegisterCompleteObjectUndo(this, "Undo Clip Data");
            }
#endif

            if (!saveCopy)
            {
                this.clips = newClips;
            }
            else
            {
                this.clips = CloneClips(newClips);
            }

            EditorSaver.SetObjectDirty(this);
        }

        private List<ClipData> CloneClips(List<ClipData> srcList)
        {
            var newClips = new List<ClipData>(srcList.Count);

            for (int i = 0; i < srcList.Count; i++)
            {
                if (srcList[i] == null)
                {
                    continue;
                }

                var copyClip = srcList[i].Clone() as ClipData;

                newClips.Add(copyClip);
            }

            return newClips;
        }
    }
}
