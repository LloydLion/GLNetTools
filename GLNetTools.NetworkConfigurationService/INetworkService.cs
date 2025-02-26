namespace GLNetTools.NetworkConfigurationService
{
	internal interface INetworkService
	{
		public void Setup(ServiceConfiguration configuration);

		public void Start();
	}
}
