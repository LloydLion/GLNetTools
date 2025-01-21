using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GLNetTools.Common.IPC
{
	public sealed class PropertyObject : IDictionary<string, PropertyObjectValue>, ICloneable, IEquatable<PropertyObject>
	{
		private readonly Dictionary<string, PropertyObjectValue> _values = [];


		public PropertyObject()
		{

		}

		public PropertyObject(PropertyObject other)
		{
			_values = new Dictionary<string, PropertyObjectValue>(other._values);
		}

		public PropertyObject(Dictionary<string, PropertyObjectValue> values)
		{
			_values = values;
		}


		public PropertyObjectValue this[string key] { get => _values[key]; set => _values[key] = value; }

		public ICollection<string> Keys => _values.Keys;

		public ICollection<PropertyObjectValue> Values => _values.Values;

		public int Count => _values.Count;

		public bool IsReadOnly => false;


		public object Clone() => new PropertyObject(this);

		public bool Equals(PropertyObject? other)
		{
			return other is not null && other._values.SequenceEqual(_values);
		}

		public override bool Equals(object? obj) => Equals(obj as PropertyObject);

		public override int GetHashCode() => _values.GetHashCode();

		public override string? ToString()
		{
			//TODO
			return base.ToString();
		}

		public void Add(string key, PropertyObjectValue value) => _values.Add(key, value);

		public void Add(KeyValuePair<string, PropertyObjectValue> item) => _values.Add(item.Key, item.Value);

		public void Clear() => _values.Clear();

		public bool Contains(KeyValuePair<string, PropertyObjectValue> item) => _values.Contains(item);

		public bool ContainsKey(string key) => _values.ContainsKey(key);

		public void CopyTo(KeyValuePair<string, PropertyObjectValue>[] array, int arrayIndex) =>
			((ICollection<KeyValuePair<string, PropertyObjectValue>>)_values).CopyTo(array, arrayIndex);

		public IEnumerator<KeyValuePair<string, PropertyObjectValue>> GetEnumerator() => _values.GetEnumerator();

		public bool Remove(string key) => _values.Remove(key);

		public bool Remove(KeyValuePair<string, PropertyObjectValue> item) =>
			((ICollection<KeyValuePair<string, PropertyObjectValue>>)_values).Remove(item);

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out PropertyObjectValue value) =>
			((IDictionary<string, PropertyObjectValue>)_values).TryGetValue(key, out value);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();
	}
}
