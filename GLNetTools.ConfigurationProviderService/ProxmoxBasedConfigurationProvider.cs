using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService
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


		public async Task FetchConfigurationAsync(ServiceConfigurationBuilder builder)
		{
			await FetchGMGroupAsync(_options.Containers, builder);
			await FetchGMGroupAsync(_options.VirtualMachines, builder);
		}

		private async Task FetchGMGroupAsync(Options.GMGroupFetchOptions loadOptions, ServiceConfigurationBuilder builder)
		{
			try
			{
				var configDirectory = Path.Combine(_options.PVEDirectoryBasePath, loadOptions.DirectorySubPath);
				var configs = Directory.EnumerateFiles(configDirectory, "*.conf");

				foreach (var config in configs)
				{
					var id = GuestMachineId.UninitializedMachine;
					try
					{
						id = new GuestMachineId(byte.Parse(Path.GetFileNameWithoutExtension(config)));

						var lines = await File.ReadAllLinesAsync(config);

						var netOption = lines.First(s => s.StartsWith(loadOptions.NetworkOptionName));
						var netParameters = netOption.Split(' ')[1].Split(',');
						var hwaddrParameter = netParameters.First(s => s.StartsWith(loadOptions.NetworkParameterName));
						var hwaddrString = hwaddrParameter.Split('=')[1];
						var hwaddr = PhysicalAddress.Parse(hwaddrString);

						var nameOption = lines.First(s => s.StartsWith(loadOptions.NameParameterName));
						var name = nameOption.Split(' ')[1];

						builder.CreateGuestMachine(id);
						builder.SetGuestMachineName(id, name);
						builder.SetGuestMachineMainInterfacePhysicalAddress(id, hwaddr);

						_logger.LogDebug("Loaded configuration PVE configuration file. Id={Id}, Location={ConfigPath}, GroupLoadOptions={LoadOptions}", id, config, loadOptions);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to load PVE configuration file, config skipped. Id={Id}, Location={ConfigPath}, GroupLoadOptions={LoadOptions}", id, config, loadOptions);
					}
				}

				_logger.LogDebug("Group finished to load. GroupLoadOptions={LoadOptions}", loadOptions);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to load part of configuration. GroupLoadOptions={LoadOptions}", loadOptions);
			}
		}


		public class Options
		{
			public string PVEDirectoryBasePath { get; init; } = "/etc/pve";


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
	}
}
