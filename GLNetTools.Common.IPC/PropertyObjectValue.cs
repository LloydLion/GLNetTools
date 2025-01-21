using System.Diagnostics.CodeAnalysis;

namespace GLNetTools.Common.IPC
{
	public readonly struct PropertyObjectValue
	{
		private static readonly object _valueForNull = new();
		private readonly object _value;


		private PropertyObjectValue(object value, PrimitiveType type)
		{
			_value = value;
			Type = type;
		}

		public PropertyObjectValue() : this(_valueForNull, PrimitiveType.Null) { }

		public PropertyObjectValue(int value) : this(value, PrimitiveType.Integer) { }

		public PropertyObjectValue(double value) : this(value, PrimitiveType.Float) { }

		public PropertyObjectValue(bool value) : this(value, PrimitiveType.Boolean) { }

		public PropertyObjectValue(string value) : this(value, PrimitiveType.String) { }

		public PropertyObjectValue(PropertyObject value) : this(value, PrimitiveType.NestedObject) { }


		public PrimitiveType Type { get; }


		public bool IsNull() => Type == PrimitiveType.Null;

		public int AsInt() => (int)_value;

		public double AsFloat() => (double)_value;

		public bool AsBoolean() => (bool)_value;

		public string AsString() => (string)_value;

		public PropertyObject AsNestedObject() => (PropertyObject)_value;

		public override bool Equals([NotNullWhen(true)] object? obj) =>
			obj is PropertyObjectValue pov && pov.Type == Type && Equals(pov._value, _value);

		public override int GetHashCode() => _value.GetHashCode();

		public override string ToString() => _value.ToString() ?? throw new NullReferenceException();


		public enum PrimitiveType : byte
		{
			Null = 0,
			Integer = 1,
			Float = 2,
			Boolean = 3,
			String = 4,
			NestedObject = 255
		}
	}
}
