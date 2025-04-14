namespace GLNetTools.Common.Configuration.JsonSerialization
{
	public interface IJsonServiceConfigurationSerializer
	{
        public string Serialize(ServiceConfiguration configuration);
        
        public ServiceConfiguration Deserialize(string serializedForm);
	}
}
