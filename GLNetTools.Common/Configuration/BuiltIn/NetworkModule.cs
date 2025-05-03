using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.Common.Configuration.BuiltIn
{
	public class NetworkModule : IConfigurationModule
	{
		public readonly static NetworkModule Instance = new();

		public readonly static ConfigurationModuleProjectionStaticPrototype<Master> MasterPrototype =
			ConfigurationModuleProjectionStaticPrototype<Master>.Create(DefaultBehavior.Instance, Instance);

		public readonly static ConfigurationModuleProjectionStaticPrototype<GuestMachine> GuestMachinePrototype =
			ConfigurationModuleProjectionStaticPrototype<GuestMachine>.Create(DefaultBehavior.Instance, Instance);


		private readonly static Dictionary<ConfigurationScopeType, ConfigurationModuleProjectionPrototype> _prototypes = new()
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
			public IPAddress FallbackDNS = IPAddress.Any;
			public string? MainInterface;
			public List<string> DNSZones = [];
		}

		public class GuestMachine() : CommonStaticModel<GuestMachine, GuestMachineId>(GuestMachinePrototype, BuiltInScopeTypes.GuestMachine)
		{
			public PhysicalAddress MainInterfacePhysicalAddress = PhysicalAddress.None;
			public List<FirewallRule> FirewallRules = [];
		}
	}
}
