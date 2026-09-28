using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**************
* Original code of this class goes to LordofDuct & JoshuaMcKenzie in the forum thread
* https://forum.unity3d.com/threads/finally-a-serializable-dictionary-for-unity-extracted-from-system-collections-generic.335797/#post-2282920
* https://forum.unity.com/threads/finally-a-serializable-dictionary-for-unity-extracted-from-system-collections-generic.335797/page-2#post-2989269
* It has since been tweaked with a couple bonus features
*************/

namespace Spirit604.Collections.Dictionary
{
    //Base, non-generic class for UnityEditor to reference Drawers
    public abstract class BaseSerializableDictionary
    {
        //Meta Data used for the Drawer labels
        public string keyName = "Key";
        public string valueName = "Value";
        public string newKeyName = "New Key";
        public bool showContent = true;
        public bool initPages = false;
        public bool showPages = true;
        public bool dirty = true;

        public BaseSerializableDictionary(string keyName = "Key", string valueName = "Value", string newKeyName = "New Key")
        {
            this.keyName = keyName;
            this.valueName = valueName;
            this.newKeyName = newKeyName;
        }
    }

    // Abstract, generic class with most implementation
    public abstract class AbstractSerializableDictionary<TKey, TValue> :
        BaseSerializableDictionary,
        IDictionary<TKey, TValue>,
        ISerializationCallbackReceiver
    {
        private const int BigArrayElementCount = 200;

        [SerializeField] private TKey newKey;//used by the Drawer when it want to add a new item (a key needs to be provided first)
        [SerializeField] private TKey[] keys;
        [SerializeField] private TValue[] values;

        [NonSerialized] private Dictionary<TKey, TValue> dict;

        private bool CollectionNotMatched => (dict != null && keys != null && dict.Keys.Count != keys.Length) || dict == null || keys == null;

        public AbstractSerializableDictionary(string keyName = "Key", string valueName = "Value", string newKeyName = "New Key")
            : base(keyName, valueName, newKeyName) { }

        public void SetDictionary(TKey[] newKeys, TValue[] newvalues)
        {
            keys = newKeys;
            values = newvalues;

            OnAfterDeserialize();
        }

        #region Linq Extras
        public void RemoveAll(Func<KeyValuePair<TKey, TValue>, bool> match)
        {
            if (dict == null) return;

            dict = dict.Where(kvp => !match(kvp)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public void ForEach(Action<KeyValuePair<TKey, TValue>> action)
        {
            if (dict == null) return;

            foreach (var item in dict)
            {
                action(item);
            }
        }
        #endregion

        #region IDictionary implementation

        public virtual bool ContainsKey(object key)
        {
            if (key == null) return false;

            return ContainsKey((TKey)key);
        }

        public bool ContainsKey(TKey key) { return (dict == null) || key == null ? false : dict.ContainsKey(key); }

        public void Add(TKey key, TValue value)
        {
            if (dict == null)
                dict = new Dictionary<TKey, TValue>();

            SetDirty();
            dict.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            if (dict == null)
            {
                return false;
            }
            else
            {
                var removed = dict.Remove(key);

                if (removed)
                {
                    SetDirty();
                }

                return removed;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (dict == null)
            {
                value = default(TValue);
                return false;
            }

            return dict.TryGetValue(key, out value);
        }

        public TValue this[TKey index]
        {
            get
            {
                if (dict == null)
                    throw new NullReferenceException();

                return dict[index];
            }
            set
            {
                if (dict == null) dict = new Dictionary<TKey, TValue>();
                dict[index] = value;
                SetDirty();
            }
        }

        public ICollection<TKey> Keys { get { if (dict == null) dict = new Dictionary<TKey, TValue>(); return dict.Keys; } }

        public ICollection<TValue> Values { get { if (dict == null) dict = new Dictionary<TKey, TValue>(); return dict.Values; } }

        #endregion

        #region ICollection implementation
        public void Add(KeyValuePair<TKey, TValue> item) { if (dict == null) dict = new Dictionary<TKey, TValue>(); dict.Add(item.Key, item.Value); }

        public void Clear()
        {
            if (dict != null)
                dict.Clear();

            keys = null;
            values = null;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) { return (dict == null) ? false : (dict as ICollection<KeyValuePair<TKey, TValue>>).Contains(item); }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (dict != null)
                (dict as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) { return (dict == null) ? false : (dict as ICollection<KeyValuePair<TKey, TValue>>).Remove(item); }

        public int Count { get { return (dict == null) ? 0 : dict.Count; } }

        public bool IsReadOnly { get { return (dict == null) ? false : (dict as ICollection<KeyValuePair<TKey, TValue>>).IsReadOnly; } }

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return (dict == null)
                ? default(Dictionary<TKey, TValue>.Enumerator)
                    : dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (dict == null)
                ? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator()
                    : dict.GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return (dict == null)
                ? Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator()
                    : dict.GetEnumerator();
        }
        #endregion

        #region ISerializationCallbackReceiver implementation

        public void OnBeforeSerialize()
        {
            if (dict == null)
            {
                keys = null;
                values = null;
                return;
            }

            if (!ShouldUpdate())
            {
                return;
            }

            dirty = false;
            keys = new TKey[dict.Count];
            values = new TValue[dict.Count];

            var e = dict.GetEnumerator();
            for (int i = 0; e.MoveNext(); i++)
            {
                keys[i] = e.Current.Key;
                values[i] = e.Current.Value;
            }
        }

        public void OnAfterDeserialize()
        {
            if (keys == null || values == null) return;

            if (!ShouldUpdate())
            {
                return;
            }

            dirty = false;
            dict = new Dictionary<TKey, TValue>(keys.Length);

            for (int i = 0; i < keys.Length; i++)
            {
                TValue value = (i >= values.Length) ? default(TValue) : values[i];

                this[keys[i]] = value;
            }
        }

        #endregion

        #region Helper methods

        public void SetDirty()
        {
            dirty = true;
        }

        public void SaveEditorData()
        {
            SetDirty();
            OnBeforeSerialize();
        }

        //Optimization for big arrays
        private bool ShouldUpdate()
        {
            return dirty || CollectionNotMatched || (keys != null && keys.Length < BigArrayElementCount);
        }

        #endregion
    }

    /// <summary>
    /// Serializable version that will be shown in the inspector, reccomended for TValue types that are not Unity Objects
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : AbstractSerializableDictionary<TKey, TValue>
    {
        public SerializableDictionary(string keyName = "Key", string valueName = "Value", string newKeyName = "New Key")
            : base(keyName, valueName, newKeyName) { }
    }

    /// <summary>
    // Serializable version that will be shown in the inspector, reccomended for TValue types that are Unity Objects
    /// </summary>
    [Serializable]
    public class SerializableObjectDictionary<TKey, TValue> : AbstractSerializableDictionary<TKey, TValue>
        where TValue : UnityEngine.Object
    {
        public SerializableObjectDictionary(string keyName = "Key", string valueName = "Value", string newKeyName = "New Key")
            : base(keyName, valueName, newKeyName)
        {
        }
    }
}