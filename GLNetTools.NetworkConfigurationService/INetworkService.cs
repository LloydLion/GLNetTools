namespace GLNetTools.NetworkConfigurationService
{
	internal interface INetworkService
	{
		public bool Setup(ServiceConfiguration configuration);

		public void Start();
	}
}
