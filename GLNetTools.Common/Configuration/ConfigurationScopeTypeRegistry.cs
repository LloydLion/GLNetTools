namespace GLNetTools.Common.Configuration
{
	public class ConfigurationScopeTypeRegistry
	{
		private readonly Dictionary<string, ConfigurationScopeType> _scopes = [];


		public void Register(ConfigurationScopeType type) => _scopes.Add(type.Name, type);

		public IReadOnlyCollection<ConfigurationScopeType> ListAll() => _scopes.Values;

		public ConfigurationScopeType GetByName(string name) => _scopes[name];
	}
}
