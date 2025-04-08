namespace GLNetTools.Common.Configuration
{
	public record ConfigurationModuleProjection(ConfigurationModuleProjectionPrototype Prototype, IReadOnlyDictionary<string, ConfigurationPropertyWithValue> Data)
	{
		public TValue Get<TValue>(string name)
		{
			if (typeof(TValue) != GetProperty(name).Type)
				throw new ArgumentException("TValue must be equal property's type (polymorphic casts not allowed)");
			return (TValue)GetWeak(name);
		}

		public object GetWeak(string name) => Data[name].Value;

		public ConfigurationProperty GetProperty(string name) => Data[name].Property;
	}
}
