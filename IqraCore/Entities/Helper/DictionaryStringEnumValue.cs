using System.Collections;
using System.Text.Json.Serialization;

namespace IqraCore.Entities.Helper
{
    public class DictionaryStringEnumValue<TKey1, TKey2, TValue> : IDictionary<string, TValue>, IInternalDictionaryProvider
    where TKey2 : Enum
    {
        private readonly Dictionary<string, TValue> _innerDictionary = new Dictionary<string, TValue>();

        [JsonIgnore] // Hide from serialization
        public ICollection<string> Keys => _innerDictionary.Keys;

        [JsonIgnore] // Hide from serialization
        public ICollection<TValue> Values => _innerDictionary.Values;

        [JsonIgnore] // Hide from serialization
        public int Count => _innerDictionary.Count;

        [JsonIgnore] // Hide from serialization
        public bool IsReadOnly => false;

        public TValue this[string key]
        {
            get => _innerDictionary[key];
            set => _innerDictionary[key] = value;
        }

        // Method to get the internal dictionary for serialization
        public Dictionary<string, TValue> GetInternalDictionary() => _innerDictionary;

        // Rest of the methods remain the same...
        public void Add(TKey2 enumKey, TValue value)
        {
            _innerDictionary.Add(enumKey.ToString(), value);
        }

        public void Add(string key, TValue value)
        {
            _innerDictionary.Add(key, value);
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            ((IDictionary<string, TValue>)_innerDictionary).Add(item);
        }

        public bool ContainsKey(string key) => _innerDictionary.ContainsKey(key);

        public bool ContainsKey(TKey2 enumKey) => _innerDictionary.ContainsKey(enumKey.ToString());

        public bool Remove(string key) => _innerDictionary.Remove(key);

        public bool TryGetValue(string key, out TValue value) =>
            _innerDictionary.TryGetValue(key, out value);

        public void Clear() => _innerDictionary.Clear();

        public bool Contains(KeyValuePair<string, TValue> item) =>
            ((IDictionary<string, TValue>)_innerDictionary).Contains(item);

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) =>
            ((IDictionary<string, TValue>)_innerDictionary).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, TValue> item) =>
            ((IDictionary<string, TValue>)_innerDictionary).Remove(item);

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() =>
            _innerDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IDictionary IInternalDictionaryProvider.GetInternalDictionary()
        {
            return _innerDictionary;
        }
    }

    public interface IInternalDictionaryProvider
    {
        IDictionary GetInternalDictionary();
    }
}
