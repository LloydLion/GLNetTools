namespace GLNetTools.Common.Configuration
{
	public class ConfigurationModuleRegistry
	{
		private readonly Dictionary<string, IConfigurationModule> _modules = [];


		public void Register(IConfigurationModule module) => _modules.Add(module.Name, module);

		public IReadOnlyCollection<IConfigurationModule> ListAll() => _modules.Values;

		public IConfigurationModule GetByName(string name) => _modules[name];
	}
}
