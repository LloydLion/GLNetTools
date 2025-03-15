using GLNetTools.NetworkConfigurationService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if DEBUG
if (args.Contains("--no-dbg"))
{
	Console.WriteLine("Waiting for debugger to attach");
	while (!System.Diagnostics.Debugger.IsAttached)
		Thread.Yield();
	Console.WriteLine("Debugger attached");
}
#endif

var services = new ServiceCollection()
	.AddLogging(s => s.AddConsole().SetMinimumLevel(LogLevel.Trace))

	.AddSingleton(new ProxmoxBasedConfigurationProvider.Options())
	.AddTransient<IServiceConfigurationProvider, ProxmoxBasedConfigurationProvider>()
	
	.AddSingleton<INetworkService, DomainNameSystemService>()
	.AddSingleton<INetworkService, DynamicHostConfigurationProtocolService>()

	.BuildServiceProvider();

var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

var configurationProvider = services.GetRequiredService<IServiceConfigurationProvider>();
var configuration = await configurationProvider.FetchConfigurationAsync();

logger.LogInformation("Using configuration: DNSZones=[{DNSZones}], FallbackDNSServer={FallbackDNSServer}, MainInterface={MainInterface}, ServerName={ServerName}",
	configuration.DNSZones, configuration.FallbackDNSServer, configuration.MainInterface?.Name, configuration.ServerName);
foreach (var gm in configuration.Machines)
	logger.LogInformation("Using guest machine configuration: Id={Id}, Name={Name}, MIPA={MIPA}", gm.Id, gm.Name, gm.MainInterfacePhysicalAddress);

var networkServices = services.GetServices<INetworkService>();
var activeServices = new List<INetworkService>();

foreach (var service in networkServices)
{
	var isSuccess = service.Setup(configuration);
	if (isSuccess)
	{
		logger.LogInformation("Network service {TypeName} has been setup", service.GetType().FullName);
		activeServices.Add(service);
	}
	else
	{
		logger.LogWarning("Network service {TypeName} setup with errors and will not be started", service.GetType().FullName);
	}
}

foreach (var service in activeServices)
{
	service.Start();
	logger.LogInformation("Network service {TypeName} has been started", service.GetType().FullName);
}

var exitEvent = new AutoResetEvent(false);
Console.CancelKeyPress += (s, e) => exitEvent.Set();
exitEvent.WaitOne();
logger.LogInformation("SIGINT detected, bye.");
