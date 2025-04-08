namespace GLNetTools.Common
{
	public record FirewallRule(
		GuestMachineId SourceMachineId,
		GuestMachineId DestinationMachineId,
		ushort SourcePort,
		ushort DestinationPort,
		FirewallProtocol Protocol,
		FirewallRule.RuleType Type)
	{
		public enum RuleType
		{
			Forward,
			DNat
		}


		public FirewallRule UseContext(GuestMachineId ContextMachineId)
		{
			return this with
			{
				SourceMachineId = SourceMachineId.UseContext(ContextMachineId),
				DestinationMachineId = DestinationMachineId.UseContext(ContextMachineId)
			};
		}
	}
}