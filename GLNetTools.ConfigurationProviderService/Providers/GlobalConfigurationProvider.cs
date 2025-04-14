using Newtonsoft.Json;
using GLNetTools.Common;
using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.BuiltIn;
using System.Net.NetworkInformation;
using System.Net;

namespace GLNetTools.ConfigurationProviderService.Providers
{
	internal class GlobalConfigurationProvider : IConfigurationProvider
	{
		private readonly Options _options;
		private readonly ILogger<GlobalConfigurationProvider> _logger;


		public GlobalConfigurationProvider(Options options, ILogger<GlobalConfigurationProvider> logger)
		{
			_options = options;
			_logger = logger;
		}

		public IConfigurationProvider.ITracker CreateTracker()
		{
			return NullTracker.Instance;
		}

		public async Task ProvideConfigurationAsync(IConfigurationBuilderAccessor builder)
		{
			builder.EnsureScopeCreated(BuiltInScopeTypes.Master, NoScopeKey.Instance);
			try
			{
				var globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(await File.ReadAllTextAsync(_options.GlobalConfigPath))
					?? throw new Exception("Invalid config");

				builder.AddProjection(new NetworkModule.Master()
				{
					DNSZones = globalConfig.DNSZones.ToList(),
					FallbackDNS = IPAddress.Parse(globalConfig.FallbackDNSServer),
					MainInterface = globalConfig.MainInterface,
				});
				builder.AddProjection(new BaseModule.Master()
				{
					HostName = globalConfig.ServerName
				});
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Failed to load global configuration (from {GlobalConfigPath}), using anti-crisis configuration", _options.GlobalConfigPath);

				builder.AddProjection(new NetworkModule.Master()
					{ FallbackDNS = IPAddress.Parse("1.1.1.1") });
				builder.AddProjection(new BaseModule.Master()
					{ HostName = "failed.to.load.configuration" });
			}
		}


		public class Options
		{
			public string GlobalConfigPath { get; init; } = "service.json";
		}

		private class GlobalConfig
		{
			public required string[] DNSZones { get; init; }

			public required string FallbackDNSServer { get; init; }

			public required string MainInterface { get; init; }

			public required string ServerName { get; init; }
		}
	}
}
