namespace GLNetTools.ConfigurationProviderService
{
	[Flags]
	internal enum FirewallProtocol : byte
	{
		UDP = 0b01,
		TCP = 0b10,
		
		Any = 0b11,
		None = 0b00
	}
}