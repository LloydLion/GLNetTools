using GLNetTools.Common.Configuration;

namespace GLNetTools.NetworkConfigurationService
{
	internal interface INetworkService
	{
		public void GetConfigurationQueryOptions(out string[] scopes, out string[] modules);

		public void StartCritical();

		public void Start(ServiceConfiguration configuration);

		public void Stop();
	}
}
