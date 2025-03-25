using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService
{
	internal class ServiceConfigurationBuilder
	{
		private Dictionary<GuestMachineId, MutableGuestMachineConfiguration> _gms = [];
		private readonly List<string> _dnsZones = [];


		public ServiceConfigurationBuilder()
		{
		
		}


		public IPAddress? FallbackDNSServer { get; set; }

		public NetworkInterface? MainInterface { get; set; }

		public string? ServerName { get; set;  }

		public ICollection<string> DNSZones => _dnsZones;


		public ServiceConfigurationBuilder CreateGuestMachine(GuestMachineId id)
		{
			if (_gms.ContainsKey(id) == false)
				_gms.Add(id, new MutableGuestMachineConfiguration());
			return this;
		}

		public ServiceConfigurationBuilder SetGuestMachineName(GuestMachineId id, string name)
		{
			_gms[id].Name = name;
			return this;
		}

		public ServiceConfigurationBuilder SetGuestMachineMainInterfacePhysicalAddress(GuestMachineId id, PhysicalAddress mainInterfacePhysicalAddress)
		{
			_gms[id].MainInterfacePhysicalAddress = mainInterfacePhysicalAddress;
			return this;
		}

		public ServiceConfigurationBuilder AddGuestMachinFirewallRule(GuestMachineId id, FirewallRule rule)
		{
			_gms[id].Rules.Add(rule);
			return this;
		}

		public ServiceConfiguration Build()
		{
			return new ServiceConfiguration(
				_gms.Select(s =>
				{
					var network = new GuestMachineNetworkConfiguration(s.Value.MainInterfacePhysicalAddress
						?? throw new ConfigurationDoNotCompletedException($"Machines[{s.Key}].NetworkConfiguration.MainInterfacePhysicalAddress"), s.Value.Rules);

					return new GuestMachineConfiguration(s.Key, s.Value.Name ?? throw new ConfigurationDoNotCompletedException($"Machines[{s.Key}].Name"), network);
				}).ToArray(),

				_dnsZones,
				FallbackDNSServer ?? throw new ConfigurationDoNotCompletedException("FallbackDNSServer"),
				MainInterface,
				ServerName ?? throw new ConfigurationDoNotCompletedException("ServerName")
			);
		}


		public class ConfigurationDoNotCompletedException : Exception
		{
			public ConfigurationDoNotCompletedException(string propertyPath)
				: base($"Configuration builder cannot build configuration: {propertyPath} is not set") { }
		}

		private class MutableGuestMachineConfiguration
		{
			public string? Name;
			public PhysicalAddress? MainInterfacePhysicalAddress;
			public List<FirewallRule> Rules = [];
		}
	}
}
