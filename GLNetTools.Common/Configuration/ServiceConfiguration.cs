namespace GLNetTools.Common.Configuration
{
	public record ServiceConfiguration(IReadOnlyCollection<ConfigurationScope> Scopes, DateTime? Version = null)
	{
		public IReadOnlyCollection<ConfigurationScopeType> ScopeTypes { get; } = Scopes.Select(s => s.WeakScopeType).Distinct().ToArray();


		public IEnumerable<ConfigurationScope> FilterScopes(ConfigurationScopeType type) => Scopes.Where(s => s.WeakScopeType == type);

		public IEnumerable<ConfigurationScope<TKey>> FilterScopes<TKey>(ConfigurationScopeType<TKey> type) where TKey : notnull
			=> FilterScopes(type).OfType<ConfigurationScope<TKey>>();

		public ConfigurationScope GetScopeWeak(ConfigurationScopeType type, object key) => Scopes.Single(s => s.WeakScopeType == type && Equals(key, s.WeakKey));

		public ConfigurationScope<TKey> GetScope<TKey>(ConfigurationScopeType<TKey> type, TKey key) where TKey : notnull
			=> (ConfigurationScope<TKey>)GetScopeWeak(type, key);
	}
}
