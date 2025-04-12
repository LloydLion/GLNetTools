namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class BaseModule : IConfigurationModule
	{
		public readonly static BaseModule Instance = new();

		public readonly static ConfigurationModuleProjectionStaticPrototype<Master> MasterPrototype =
			ConfigurationModuleProjectionStaticPrototype<Master>.Create(DefaultBehavior.Instance, Instance);

		public readonly static ConfigurationModuleProjectionStaticPrototype<GuestMachine> GuestMachinePrototype =
			ConfigurationModuleProjectionStaticPrototype<GuestMachine>.Create(DefaultBehavior.Instance, Instance);


		private readonly Dictionary<ConfigurationScopeType, ConfigurationModuleProjectionPrototype> _prototypes = new()
		{
			[BuiltInScopeTypes.Master] = MasterPrototype,
			[BuiltInScopeTypes.GuestMachine] = GuestMachinePrototype
		};


		public string Name { get; } = "Network";


		public ConfigurationModuleProjectionPrototype? ProvidePrototypeFor(ConfigurationScopeType scope)
		{
			if (_prototypes.TryGetValue(scope, out var prototype))
				return prototype;
			else return null;
		}


		public class Master() : CommonStaticModel<Master, NoScopeKey>(MasterPrototype, BuiltInScopeTypes.Master)
		{
			public string HostName = string.Empty;
		}

		public class GuestMachine() : CommonStaticModel<GuestMachine, GuestMachineId>(GuestMachinePrototype, BuiltInScopeTypes.GuestMachine)
		{
			public string HostName = string.Empty;
		}
	}
}
