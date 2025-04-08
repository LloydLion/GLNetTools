namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class BaseModule : IConfigurationModule
	{
		public readonly static BaseModule Instance = new();

		public readonly static ConfigurationModuleProjectionStaticPrototype<Master> MasterPrototype =
			ConfigurationModuleProjectionStaticPrototype<Master>.Create(DefaultBehavior.Instance, Instance);

		public readonly static ConfigurationModuleProjectionStaticPrototype<GuestMachine> GuestMachinePrototype =
			ConfigurationModuleProjectionStaticPrototype<GuestMachine>.Create(DefaultBehavior.Instance, Instance);


		private readonly Dictionary<string, ConfigurationModuleProjectionPrototype> _prototypes = new()
		{
			["Master"] = MasterPrototype,
			["GuestMachine"] = GuestMachinePrototype
		};


		public string Name { get; } = "Network";


		public ConfigurationModuleProjectionPrototype? ProvidePrototypeFor(ConfigurationScopeType scope)
		{
			if (_prototypes.TryGetValue(scope.Name, out var prototype))
				return prototype;
			else return null;
		}


		public class Master
		{
			public string HostName = string.Empty;
		}

		public class GuestMachine
		{
			public string HostName = string.Empty;
		}
	}
}
