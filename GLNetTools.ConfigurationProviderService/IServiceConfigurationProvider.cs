namespace GLNetTools.ConfigurationProviderService
{
	internal interface IServiceConfigurationProvider
	{
		public Task FetchConfigurationAsync(ServiceConfigurationBuilder builder);
	}
}
