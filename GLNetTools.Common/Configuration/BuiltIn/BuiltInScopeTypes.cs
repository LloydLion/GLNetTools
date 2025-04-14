namespace GLNetTools.Common.Configuration.BuiltIn
{
	public static class BuiltInScopeTypes
	{
		public static ConfigurationScopeType<GuestMachineId> GuestMachine { get; } = new ConfigurationScopeType<GuestMachineId>(nameof(GuestMachine))
		    { Serializer = new MappedSerializer<GuestMachineId>(s => s.Id.ToString(), s => new GuestMachineId(byte.Parse(s))) };

		public static ConfigurationScopeType<NoScopeKey> Master { get; } = new ConfigurationScopeType<NoScopeKey>(nameof(Master))
            { Serializer = new MappedSerializer<NoScopeKey>(s => "", s => NoScopeKey.Instance) };


        private class MappedSerializer<TObject> : IConfigurationScopeKeySerializer where TObject : notnull
        {
			private readonly Func<TObject, string> _forward;
			private readonly Func<string, TObject> _backward;


            public MappedSerializer(Func<TObject, string> forward, Func<string, TObject> backward)
            {
                _forward = forward;
                _backward = backward;
            }


            public bool Compare(object key, string rawValue) => _forward((TObject)key) == rawValue;

            public object Deserialize(string rawValue) => _backward(rawValue);

            public string Serialize(object key) => _forward((TObject)key);
        }
    }
}
