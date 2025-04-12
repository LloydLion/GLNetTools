using GLNetTools.Common;
using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.BuiltIn;
using System.Net.NetworkInformation;

namespace GLNetTools.ConfigurationProviderService.Providers
{
	internal class ProxmoxBasedConfigurationProvider : IConfigurationProvider
	{
		private readonly Options _options;
		private readonly ILogger<ProxmoxBasedConfigurationProvider> _logger;


		public ProxmoxBasedConfigurationProvider(Options options, ILogger<ProxmoxBasedConfigurationProvider> logger)
		{
			_options = options;
			_logger = logger;
		}

		public IConfigurationProvider.ITracker CreateTracker()
		{
			var vmWatcher = new FileSystemWatcher(Path.Combine(_options.PVEDirectoryBasePath, _options.VirtualMachines.DirectorySubPath));
			var ctWatcher = new FileSystemWatcher(Path.Combine(_options.PVEDirectoryBasePath, _options.Containers.DirectorySubPath));

			var tracker = new Tracker(this, vmWatcher, ctWatcher);
			tracker.Init();

			return tracker;
		}

		public async Task ProvideConfigurationAsync(IConfigurationBuilderAccessor builder)
		{
			await FetchGMGroupAsync(_options.Containers, builder);
			await FetchGMGroupAsync(_options.VirtualMachines, builder);
		}

		private async Task FetchGMGroupAsync(Options.GMGroupFetchOptions loadOptions, IConfigurationBuilderAccessor builder)
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

						builder.EnsureScopeCreated(BuiltInScopeTypes.GuestMachine, id);
						builder.AddProjection(id, new BaseModule.GuestMachine()
						{
							HostName = name
						});
						builder.AddProjection(id, new NetworkModule.GuestMachine()
						{
							MainInterfacePhysicalAddress = hwaddr,
						});

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


		private class Tracker : IConfigurationProvider.ITracker
		{
			private Action<IConfigurationProvider>? _callback;
			private bool _enabled = false;
			private readonly FileSystemWatcher[] _watchers;
			private readonly IConfigurationProvider _owner;


			public Tracker(IConfigurationProvider owner, params FileSystemWatcher[] watchers)
			{
				_watchers = watchers;
				_owner = owner;
			}


			public void Init()
			{
				foreach (var watcher in _watchers)
				{
					watcher.Changed += WatcherFileChanged;
					watcher.Created += WatcherFileChanged;
					watcher.Created += WatcherFileChanged;
					watcher.Deleted += WatcherFileChanged;
				}
			}

			private void WatcherFileChanged(object sender, FileSystemEventArgs e)
			{
				if (_enabled && _callback is not null)
					_callback.Invoke(_owner);
			}

			public void SetCallback(Action<IConfigurationProvider> callback)
			{
				_callback = callback;
			}

			public void StartTracking()
			{
				_enabled = true;
			}

			public void StopTracking()
			{
				_enabled = false;
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
