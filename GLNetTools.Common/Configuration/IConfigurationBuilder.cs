namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationBuilder : IConfigurationBuilderAccessor
	{
		public void SetActiveProvider(IConfigurationProvider? provider);

		public ServiceConfiguration Build();

		public void RemovePartsCreatedBy(IConfigurationProvider provider);

		public void TakeSnapshot();

		public void Rollback();


	}

	public interface IConfigurationBuilderAccessor
	{
		public void EnsureScopeCreated(ConfigurationScopeType type, object key);

		public void AddProjection(ConfigurationScopeType type, object key, ConfigurationModuleProjection projection);
	}
}
