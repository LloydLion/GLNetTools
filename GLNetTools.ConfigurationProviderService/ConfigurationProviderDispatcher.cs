namespace GLNetTools.ConfigurationProviderService
{
	internal class ConfigurationProviderDispatcher
	{
		private readonly IEnumerable<IConfigurationProvider> _providers;
		private readonly ILogger<ConfigurationProviderDispatcher> _logger;
		private readonly List<IConfigurationProvider.ITracker> _trackers = [];
		private ConfigurationHolder? _holderForTracking;


		public ConfigurationProviderDispatcher(IEnumerable<IConfigurationProvider> providers, ILogger<ConfigurationProviderDispatcher> logger)
		{
			_providers = providers;
			_logger = logger;
		}


		public async Task PerformInitialConfigurationLoadAsync(ConfigurationHolder holder)
		{
			_logger.LogInformation("Configuration initial load started");

			int fails = 0;
			await holder.AccessBuilderAsync(async builder =>
			{
				foreach (var provider in _providers)
				{
					try
					{
						builder.SetActiveProvider(provider);
						await provider.ProvideConfigurationAsync(builder);
						_logger.LogDebug("Provider [{Provider}] loaded initial configuration", provider);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "During configuration initial load provider [{Provider}] thrown an exception", provider);
						fails++;
					}
				}
				return 0;
			});

			_logger.LogInformation("Configuration initial load completed. Skipped providers: {Fails}", fails);
		}

		public void InitializeTracking(ConfigurationHolder holder)
		{
			if (_holderForTracking is not null)
				throw new InvalidOperationException("Already initialized for tracking");
			_holderForTracking = holder;

			foreach (var provider in _providers)
			{
				var tracker = provider.CreateTracker();
				tracker.SetCallback(TrackerCallback);
				_trackers.Add(tracker);
			}

			_logger.LogInformation("Configuration tracking initialized");
		}

		public void EnableTracking()
		{
			if (_holderForTracking is null)
				throw new InvalidOperationException("Not initialized for tracking");
			_trackers.ForEach(s => s.StartTracking());
			_logger.LogInformation("Configuration tracking enabled");
		}

		public void DisableTracking()
		{
			if (_holderForTracking is null)
				throw new InvalidOperationException("Not initialized for tracking");
			_trackers.ForEach(s => s.StopTracking());
			_logger.LogInformation("Configuration tracking disabled");
		}

		private async void TrackerCallback(IConfigurationProvider provider)
		{
			var updateId = Guid.NewGuid();
			_logger.LogDebug("Provider [{Provider}] triggered a configuration update. UpdateId={UpdateId}", provider, updateId);
			var holder = _holderForTracking!;
			bool isBuilderRolledBack = false;
			try
			{
				await holder.AccessBuilderAsync(async (builder) =>
				{
					builder.TakeSnapshot();
					try
					{
						builder.RemovePartsCreatedBy(provider);
						builder.SetActiveProvider(provider);
						await provider.ProvideConfigurationAsync(builder);
						return 0;
					}
					catch (Exception)
					{
						builder.Rollback();
						isBuilderRolledBack = true;
						throw;
					}
				});

				holder.RebuildConfiguration();
				_logger.LogInformation("Configuration update done. UpdateId={UpdateId}", updateId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during configuration update. Configuration rolled back?={RollbackStatus}. UpdateId={UpdateId}",
					isBuilderRolledBack, updateId);
			}
		}
	}
}
