namespace GLNetTools.Common.Configuration
{
	public interface IConfigurationProvider
	{
		public ITracker CreateTracker();

		public Task ProvideConfigurationAsync(IConfigurationBuilder builder);


		public interface ITracker
		{
			public void StartTracking();

			public void StopTracking();

			public void SetCallback(Action<IConfigurationProvider> callback);
		}
	}
}
