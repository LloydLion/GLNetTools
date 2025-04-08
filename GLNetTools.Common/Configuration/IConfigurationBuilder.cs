namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationBuilder
	{
		public void SetActiveProvider(IConfigurationProvider? provider);

		public void EnsureScopeCreated(ConfigurationScopeType type, object key);

		public void AddProjection(ConfigurationScopeType type, object key, ConfigurationModuleProjection projection);

		public ServiceConfiguration Build();

		public void RemovePartsCreatedBy(IConfigurationProvider provider);
	}
}
