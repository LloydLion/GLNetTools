using GLNetTools.Common.Configuration;

namespace GLNetTools.ConfigurationProviderService
{
	internal class ConfigurationHolder
	{
		private ServiceConfiguration? _configuration;
		private readonly IConfigurationBuilder _builder;
		private readonly ILogger<ConfigurationHolder> _logger;
		private readonly SemaphoreSlim _builderLock = new(1);
		private readonly List<Action<ServiceConfiguration>> RebuiltHandlers = [];


		public ConfigurationHolder(IConfigurationBuilder builder, ILogger<ConfigurationHolder> logger)
		{
			_builder = builder;
			_logger = logger;
		}


		public ServiceConfiguration Configuration => _configuration ?? throw new InvalidOperationException("Build configuration first");


		public async Task<TResult> AccessBuilderAsync<TResult>(Func<IConfigurationBuilder, Task<TResult>> action)
		{
			await _builderLock.WaitAsync();
			_logger.LogDebug("Access to configuration builder granted to {Action}", action.Method);
			try
			{
				return await action(_builder);
			}
			finally
			{
				_builderLock.Release();
			}
		}

		public Task<ServiceConfiguration> AwaitForNewConfiguration(Predicate<ServiceConfiguration> predicate, bool checkCurrent = false)
		{
			_builderLock.Wait();
			try
			{

				var currentConfig = Configuration;
				if (checkCurrent && predicate(currentConfig))
					return Task.FromResult(currentConfig);

				var tcs = new TaskCompletionSource<ServiceConfiguration>();
				RebuiltHandlers.Add(handler);
				return tcs.Task;


				void handler(ServiceConfiguration config)
				{
					if (predicate(config))
					{
						tcs.SetResult(config);
					}
				};
			}
			finally
			{
				_builderLock.Release();
			}
		}

		public void RebuildConfiguration()
		{
			_builderLock.Wait();
			try
			{
				var config = _builder.Build();
				_configuration = config;

				RebuiltHandlers.ForEach(s => s(config));
				RebuiltHandlers.Clear();

				_logger.LogInformation("Configuration has been rebuilt");
			}
			finally
			{
				_builderLock.Release();
			}
		}
	}
}
