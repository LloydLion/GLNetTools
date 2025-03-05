using GLNetTools.NetworkConfigurationService;

#if DEBUG
Console.WriteLine("Waiting for debugger to attach");
while (!System.Diagnostics.Debugger.IsAttached)
    Thread.Yield();
Console.WriteLine("Debugger attached");
#endif

IServiceConfigurationProvider configurationProvider = new ProxmoxBasedConfigurationProvider(new ProxmoxBasedConfigurationProvider.Options());

var configuration = await configurationProvider.FetchConfigurationAsync();

Console.WriteLine($"Using: {configuration}");

var services = new INetworkService[]
{
	new DomainNameSystemService(),
	new DynamicHostConfigurationProtocolService()
};

foreach (var service in services)
	service.Setup(configuration);

foreach (var service in services)
	service.Start();

var exitEvent = new AutoResetEvent(false);
Console.CancelKeyPress += (s, e) => exitEvent.Set();
exitEvent.WaitOne();
