namespace GLNetTools.NetworkConfigurationService
{
	internal interface IServiceConfigurationProvider
	{
		public Task<ServiceConfiguration> FetchConfigurationAsync();
	}
}
