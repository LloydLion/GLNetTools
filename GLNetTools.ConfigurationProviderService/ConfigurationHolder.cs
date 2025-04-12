using GLNetTools.Common.Configuration;

namespace GLNetTools.ConfigurationProviderService
{
	internal class ConfigurationHolder
	{
		private ServiceConfiguration? _configuration;
		private readonly IConfigurationBuilder _builder;
		private readonly ILogger<ConfigurationHolder> _logger;
		private readonly SemaphoreSlim _builderLock = new(1);


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

		public void RebuildConfiguration()
		{
			_configuration = _builder.Build();
			_logger.LogInformation("Configuration has been rebuilt");
		}
	}
}
