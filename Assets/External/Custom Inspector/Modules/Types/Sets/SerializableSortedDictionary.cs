using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace CustomInspector
{
    /// <summary>
    /// A serializable implementation of System.SortedDictionary
    /// Time complexity: access = O(log(n)) , add/remove = O(n)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class SerializableSortedDictionary<TKey, TValue> : SerializableDictionary<TKey, TValue>
                                                                where TKey : IComparable
    {
        readonly SerializableSortedSet<TKey> keys_casted;

        public SerializableSortedDictionary()
        : base(new SerializableSortedSet<TKey>(), new List<TValue>())
        {
            keys_casted = (SerializableSortedSet<TKey>)base.keys;
        }
        public SerializableSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : this()
        {
            foreach (var item in collection)
            {
                this.Add(item.Key, item.Value);
            }
        }


        /// <returns>True, if key/value pair got added at key. False if key already exists</returns>
        public override bool TryAdd(TKey key, TValue value)
        {
            if (key is null)
            {
                throw new ArgumentNullException("Key is null");
            }

            int ind = keys_casted.AddAndGetIndex(key);
            if (ind != -1)
            {
                values.Insert(ind, value);
                return true;
            }
            return false;
        }

        protected override void Internal_OnDeserialization(object sender)
        {
            base.Internal_OnDeserialization(sender);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (keys_casted.Count != base.keys.Count)
            {
                Debug.LogError("Deserialized keys were out of sync. Keys may lose their order");
                base.keys = keys_casted;
            }
            else
            {
                for (int i = 0; i < base.keys.Count; i++)
                {
                    if (!keys_casted[i].Equals(base.keys[i]))
                    {
                        Debug.LogError("Deserialized keys were out of sync. Keys may lose their order");
                        base.keys = keys_casted;
                        break;
                    }
                }
            }
#endif
            base.keys = keys_casted;
        }
    }
}