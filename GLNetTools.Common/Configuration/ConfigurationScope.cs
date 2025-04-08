namespace GLNetTools.Common.Configuration
{
	public abstract record ConfigurationScope(ConfigurationScopeType WeakScopeType, object WeakKey, IReadOnlyDictionary<string, ConfigurationModuleProjection> Projections)
	{
		public ConfigurationModuleProjection GetProjectionWeak(IConfigurationModule module)
		{
			return Projections[module.Name];
		}

		public TStaticModel GetProjection<TStaticModel>(IConfigurationModule module, ConfigurationModuleProjectionStaticPrototype<TStaticModel> prototype)
			where TStaticModel : class
		{
			return prototype.MapProjectionToStaticModel(GetProjectionWeak(module));
		}

		public static ConfigurationScope CreateWeak(ConfigurationScopeType WeakScopeType, object WeakKey,
			IReadOnlyDictionary<string, ConfigurationModuleProjection> Projections)
		{
			return (ConfigurationScope)typeof(ConfigurationScope<>)
				.MakeGenericType(WeakScopeType.KeyType)
				.GetConstructors().First(s => s.GetParameters().Length == 3)
				!.Invoke([WeakScopeType, WeakKey, Projections]);
		}
	}


	public record ConfigurationScope<TKey>(ConfigurationScopeType<TKey> ScopeType, TKey Key,
		IReadOnlyDictionary<string, ConfigurationModuleProjection> Projections) : ConfigurationScope(ScopeType, Key, Projections) where TKey : notnull;
}
