using System.Net.NetworkInformation;

namespace GLNetTools.NetworkConfigurationService
{
	internal record GuestMachineConfiguration(byte Id, string Name, PhysicalAddress MainInterfacePhysicalAddress);
}
