using GLNetTools.NetworkConfigurationService;

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
