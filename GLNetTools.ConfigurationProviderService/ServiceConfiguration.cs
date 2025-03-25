using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService
{
	internal record ServiceConfiguration(
		IReadOnlyCollection<GuestMachineConfiguration> Machines,
		IReadOnlyCollection<string> DNSZones,
		IPAddress FallbackDNSServer,
		NetworkInterface? MainInterface,
		string ServerName
	);
}
