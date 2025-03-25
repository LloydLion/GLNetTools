
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService
{
	internal class GlobalConfigurationProvider : IServiceConfigurationProvider
	{
		private readonly Options _options;
		private readonly ILogger<GlobalConfigurationProvider> _logger;


		public GlobalConfigurationProvider(Options options, ILogger<GlobalConfigurationProvider> logger)
		{
			_options = options;
			_logger = logger;
		}


		public async Task FetchConfigurationAsync(ServiceConfigurationBuilder builder)
		{
			try
			{
				var globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(await File.ReadAllTextAsync(_options.GlobalConfigPath))
					?? throw new Exception("Invalid config");

				foreach (var item in globalConfig.DNSZones)
					builder.DNSZones.Add(item);
				builder.FallbackDNSServer = IPAddress.Parse(globalConfig.FallbackDNSServer);
				builder.MainInterface = NetworkInterface.GetAllNetworkInterfaces().First(s => s.Name == globalConfig.MainInterface);
				builder.ServerName = globalConfig.ServerName;
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Failed to load global configuration (from {GlobalConfigPath}), using anti-crisis configuration", _options.GlobalConfigPath);
				builder.FallbackDNSServer ??= IPAddress.Parse("1.1.1.1");
				builder.ServerName ??= "failed.to.load.configuration";
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
