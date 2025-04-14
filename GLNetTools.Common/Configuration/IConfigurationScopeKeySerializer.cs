namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationScopeKeySerializer
	{
		public string Serialize(object key);

        public object Deserialize(string rawValue);

        public bool Compare(object key, string rawValue);
	}
}
