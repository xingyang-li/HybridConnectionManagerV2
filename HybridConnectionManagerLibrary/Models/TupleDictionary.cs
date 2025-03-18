namespace HybridConnectionManager.Library
{
    public class TupleDictionary<K1, K2, V> : Dictionary<Tuple<K1, K2>, V>
        where K1 : notnull
        where K2 : notnull
    {
        new public V this[K1 key1, K2 key2]
        {
            get
            {
                return base[new(key1, key2)];
            }
            set
            {
                base[new(key1, key2)] = value;
            }
        }

        public bool ContainsKeys(K1 key1, K2 key2) => base.ContainsKey(new(key1, key2));

        public bool Remove(K1 key1, K2 key2) => base.Remove(new(key1, key2));

        public bool Remove(K1 key1, K2 key2, out V value) => base.Remove(new(key1, key2), out value);

        public void Add(K1 key1, K2 key2, V value) => base.Add(new(key1, key2), value);
    }
}