namespace GLNetTools.Common.Configuration
{
	public class ConfigurationBuilder : IConfigurationBuilder
	{
		private List<RWScope> _scopes = [];
		private List<RWScope> _snapshot = [];
		private IConfigurationProvider? _activeProvider;
	
	
		public void AddProjection(ConfigurationScopeType type, object key, ConfigurationModuleProjection projection)
		{
			if (_activeProvider is null)
				throw new InvalidOperationException("No active provider");
			var scope = _scopes.SingleOrDefault(s => s.ScopeType == type && Equals(key, s.Key))
				?? throw new NullReferenceException("No scope in builder, call EnsureScopeCreated first");

			var proto = projection.Prototype;
			var module = proto.Module.Name;

			if (scope.Projections.TryGetValue(module, out var parts) == false)
				scope.Projections.Add(module, parts = new ProjectionParts(proto, []));

			parts.Parts.Add(_activeProvider, projection);
		}

		public void EnsureScopeCreated(ConfigurationScopeType type, object key)
		{
			if (_scopes.Any(s => s.ScopeType == type && Equals(key, s.Key)))
				return;
			_scopes.Add(new RWScope(type, key, []));
		}

		public void RemovePartsCreatedBy(IConfigurationProvider provider)
		{
			foreach (var scope in _scopes)
				foreach (var parts in scope.Projections.Values)
					parts.Parts.Remove(provider);
		}

		public void SetActiveProvider(IConfigurationProvider? provider)
		{
			_activeProvider = provider;
		}

		public ServiceConfiguration Build()
		{
			return new ServiceConfiguration(_scopes.Select(scope =>
			{
				var projections = scope.Projections.Select(parts =>
				{
					var projection = parts.Value.Prototype.SumProjections(parts.Value.Parts.Values);
					return KeyValuePair.Create(parts.Key, projection);
				}).ToDictionary(s => s.Key, s => s.Value);

				return ConfigurationScope.CreateWeak(scope.ScopeType, scope.Key, projections);
			}).ToArray());
		}

		public void TakeSnapshot()
		{
			// Deep _scopes clone
			_snapshot = _scopes.Select(s => s with
			{
				Projections = s.Projections.ToDictionary(s => s.Key, s => s.Value with
				{
					Parts = s.Value.Parts.ToDictionary()
				})
			}).ToList();
		}

		public void Rollback()
		{
			_scopes = _snapshot;
		}


		private record RWScope(ConfigurationScopeType ScopeType, object Key, Dictionary<string, ProjectionParts> Projections);

		private record ProjectionParts(ConfigurationModuleProjectionPrototype Prototype, Dictionary<IConfigurationProvider, ConfigurationModuleProjection> Parts);
	}
}
