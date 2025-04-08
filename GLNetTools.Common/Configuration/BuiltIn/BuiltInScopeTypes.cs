namespace GLNetTools.Common.Configuration.BuiltIn
{
	public static class BuiltInScopeTypes
	{
		public static ConfigurationScopeType<GuestMachineId> GuestMachine { get; } = new ConfigurationScopeType<GuestMachineId>(nameof(GuestMachine));

		public static ConfigurationScopeType<NoScopeKey> Master { get; } = new ConfigurationScopeType<NoScopeKey>(nameof(Master));
	}
}
