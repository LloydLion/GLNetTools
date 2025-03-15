using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.NetworkConfigurationService
{
	internal record ServiceConfiguration(
		IReadOnlyCollection<GuestMachineConfiguration> Machines,
		IReadOnlyCollection<string> DNSZones,
		IPAddress FallbackDNSServer,
		NetworkInterface? MainInterface,
		string ServerName
	);
}
