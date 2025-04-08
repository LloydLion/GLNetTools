namespace GLNetTools.Common
{
	[Flags]
	public enum FirewallProtocol : byte
	{
		UDP = 0b01,
		TCP = 0b10,
		
		Any = 0b11,
		None = 0b00
	}
}