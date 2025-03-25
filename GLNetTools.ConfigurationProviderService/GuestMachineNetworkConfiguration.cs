using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService
{
	internal record GuestMachineNetworkConfiguration(PhysicalAddress MainInterfacePhysicalAddress, IReadOnlyCollection<FirewallRule> Rules);
}
