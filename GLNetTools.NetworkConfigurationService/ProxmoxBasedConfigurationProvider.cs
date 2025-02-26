
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace GLNetTools.NetworkConfigurationService
{
	internal class ProxmoxBasedConfigurationProvider : IServiceConfigurationProvider
	{
		private readonly Options _options;


		public ProxmoxBasedConfigurationProvider(Options options)
		{
			_options = options;
		}


		public async Task<ServiceConfiguration> FetchConfigurationAsync()
		{
			var guestMachines = new List<GuestMachineConfiguration>();
			guestMachines.AddRange(await FetchGMGroupAsync(_options.Containers));
			guestMachines.AddRange(await FetchGMGroupAsync(_options.VirtualMachines));

			var globalConfig = JsonConvert.DeserializeObject<GlobalConfig>(await File.ReadAllTextAsync(_options.GlobalConfigPath))
				?? throw new Exception("Invalid config");

			return new ServiceConfiguration(guestMachines,
				globalConfig.DNSZone,
				IPAddress.Parse(globalConfig.FallbackDNSServer),
				NetworkInterface.GetAllNetworkInterfaces().First(s => s.Name == globalConfig.MainInterface),
				globalConfig.ServerName
			);
		}

		private async Task<IReadOnlyCollection<GuestMachineConfiguration>> FetchGMGroupAsync(Options.GMGroupFetchOptions loadOptions)
		{
			var configDirectory = Path.Combine(_options.PVEDirectoryBasePath, loadOptions.DirectorySubPath);
			var configs = Directory.EnumerateFiles(configDirectory, "*.conf");

			var result = new List<GuestMachineConfiguration>();
			foreach (var config in configs)
			{
				var id = int.Parse(Path.GetFileNameWithoutExtension(config));

				var lines = await File.ReadAllLinesAsync(config);

				var netOption = lines.First(s => s.StartsWith(loadOptions.NetworkOptionName));
				var netParameters = netOption.Split(' ')[1].Split(',');
				var hwaddrParameter = netParameters.First(s => s.StartsWith(loadOptions.NetworkParameterName));
				var hwaddrString = hwaddrParameter.Split('=')[1];
				var hwaddr = PhysicalAddress.Parse(hwaddrString);

				var nameOption = lines.First(s => s.StartsWith(loadOptions.NameParameterName));
				var name = nameOption.Split(' ')[1];

				result.Add(new GuestMachineConfiguration(id, name, hwaddr));
			}

			return result;
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
