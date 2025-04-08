namespace GLNetTools.Common.Configuration
{
	public abstract record ConfigurationScopeType(Type KeyType, string Name)
	{
		public static ConfigurationScopeType CreateWeak(Type KeyType, string Name)
		{
			return (ConfigurationScopeType)typeof(ConfigurationScopeType<>)
				.MakeGenericType(KeyType)
				.GetConstructor([typeof(string)])!.Invoke([Name]);
		}
	}

	public record ConfigurationScopeType<TKey>(string Name) : ConfigurationScopeType(typeof(TKey), Name) where TKey : notnull;
}
