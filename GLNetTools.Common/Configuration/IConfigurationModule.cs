namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationModule
	{
		public string Name { get; }


		public ConfigurationModuleProjectionPrototype? ProvidePrototypeFor(ConfigurationScopeType scope);
	}
}
