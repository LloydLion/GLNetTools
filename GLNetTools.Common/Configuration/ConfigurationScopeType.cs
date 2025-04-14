namespace GLNetTools.Common.Configuration
{
	public abstract record ConfigurationScopeType(Type KeyType, string Name)
	{
        private IConfigurationScopeKeySerializer? _serializer;


        public static ConfigurationScopeType CreateWeak(Type KeyType, string Name, IConfigurationScopeKeySerializer? serializer = null)
		{
			var scopeType = (ConfigurationScopeType)typeof(ConfigurationScopeType<>)
				.MakeGenericType(KeyType)
				.GetConstructor([typeof(string)])!.Invoke([Name]);
			scopeType._serializer = serializer;
			return scopeType;
		}


        public IConfigurationScopeKeySerializer? Serializer { get => _serializer; init => _serializer = value; }
    }

	public record ConfigurationScopeType<TKey>(string Name) : ConfigurationScopeType(typeof(TKey), Name) where TKey : notnull;
}
