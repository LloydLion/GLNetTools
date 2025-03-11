using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.NetworkConfigurationService
{
	internal class ProxmoxBasedConfigurationProvider : IServiceConfigurationProvider
	{
		private readonly Options _options;
		private readonly ILogger<ProxmoxBasedConfigurationProvider> _logger;


		public ProxmoxBasedConfigurationProvider(Options options, ILogger<ProxmoxBasedConfigurationProvider> logger)
		{
			_options = options;
			_logger = logger;
		}


		public async Task<ServiceConfiguration> FetchConfigurationAsync()
		{
			var guestMachines = new List<GuestMachineConfiguration>();
			guestMachines.AddRange(await FetchGMGroupAsync(_options.Containers));
			guestMachines.AddRange(await FetchGMGroupAsync(_options.VirtualMachines));
			try
			{
				var globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(await File.ReadAllTextAsync(_options.GlobalConfigPath))
					?? throw new Exception("Invalid config");

				return new ServiceConfiguration(guestMachines,
					globalConfig.DNSZone,
					IPAddress.Parse(globalConfig.FallbackDNSServer),
					NetworkInterface.GetAllNetworkInterfaces().First(s => s.Name == globalConfig.MainInterface),
					globalConfig.ServerName
				);
			}
			catch (Exception ex)
			{
				_logger.LogCritical(ex, "Failed to load global configuration (from {GlobalConfigPath}), using anti-crisis configuration", _options.GlobalConfigPath);
				return new ServiceConfiguration(guestMachines, "failed", IPAddress.Parse("1.1.1.1"), null, "failed.to.load.configuration");
			}
		}

		private async Task<IReadOnlyCollection<GuestMachineConfiguration>> FetchGMGroupAsync(Options.GMGroupFetchOptions loadOptions)
		{
			try
			{
				var configDirectory = Path.Combine(_options.PVEDirectoryBasePath, loadOptions.DirectorySubPath);
				var configs = Directory.EnumerateFiles(configDirectory, "*.conf");

				var result = new List<GuestMachineConfiguration>();
				foreach (var config in configs)
				{
					byte id = 0;
					try
					{
						id = byte.Parse(Path.GetFileNameWithoutExtension(config));

						var lines = await File.ReadAllLinesAsync(config);

						var netOption = lines.First(s => s.StartsWith(loadOptions.NetworkOptionName));
						var netParameters = netOption.Split(' ')[1].Split(',');
						var hwaddrParameter = netParameters.First(s => s.StartsWith(loadOptions.NetworkParameterName));
						var hwaddrString = hwaddrParameter.Split('=')[1];
						var hwaddr = PhysicalAddress.Parse(hwaddrString);

						var nameOption = lines.First(s => s.StartsWith(loadOptions.NameParameterName));
						var name = nameOption.Split(' ')[1];

						result.Add(new GuestMachineConfiguration(id, name, hwaddr));
						_logger.LogDebug("Loaded configuration PVE configuration file. Id={Id}, Location={ConfigPath}, GroupLoadOptions={LoadOptions}", id, config, loadOptions);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to load PVE configuration file, config skipped. Id={Id}, Location={ConfigPath}, GroupLoadOptions={LoadOptions}", id, config, loadOptions);
					}
				}

				_logger.LogDebug("Group finished to load. GroupLoadOptions={LoadOptions}", loadOptions);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load part of configuration. GroupLoadOptions={LoadOptions}", loadOptions);
				return [];
			}
		}


		public class Options
		{
			public string PVEDirectoryBasePath { get; init; } = "/etc/pve";

			public string GlobalConfigPath { get; init; } = "service.json";


			public GMGroupFetchOptions VirtualMachines { get; init; } = new()
			{
				DirectorySubPath = "qemu-server",
				NetworkOptionName = "net0",
				NetworkParameterName = "virtio",
				NameParameterName = "name",
			};

			public GMGroupFetchOptions Containers { get; init; } = new()
			{
				DirectorySubPath = "lxc",
				NetworkOptionName = "net0",
				NetworkParameterName = "hwaddr",
				NameParameterName = "hostname"
			};


			public class GMGroupFetchOptions
			{
				public required string DirectorySubPath { get; init; }

				public required string NetworkOptionName { get; init; }

				public required string NetworkParameterName { get; init; }

				public required string NameParameterName { get; init; }


				public override string ToString()
				{
					return $"{{DirectorySubPath={DirectorySubPath}, NetworkOptionName={NetworkOptionName}, " +
						$"NetworkParameterName={NetworkParameterName}, NameParameterName={NameParameterName}}}";
				}
			}
		}

		private class GlobalConfig
		{
			public required string DNSZone { get; init; }

			public required string FallbackDNSServer { get; init; }

			public required string MainInterface { get; init; }

			public required string ServerName { get; init; }
		}
	}
}
