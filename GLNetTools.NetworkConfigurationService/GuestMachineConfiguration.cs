using System.Net.NetworkInformation;

namespace GLNetTools.NetworkConfigurationService
{
	internal record GuestMachineConfiguration(int Id, string Name, PhysicalAddress MainInterfacePhysicalAddress);
}
