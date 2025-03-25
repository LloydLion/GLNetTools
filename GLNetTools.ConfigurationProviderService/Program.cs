using GLNetTools.ConfigurationProviderService;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#if DEBUG
if (args.Contains("--no-dbg") == false)
{
	Console.WriteLine("Waiting for debugger to attach");
	while (!System.Diagnostics.Debugger.IsAttached)
		Thread.Yield();
	Console.WriteLine("Debugger attached");
}
#endif


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvc().AddNewtonsoftJson(options =>
{
	options.SerializerSettings.Converters.Add(new StringEnumConverter());
	options.SerializerSettings.Converters.Add(new ServiceConfigurationConverter());
	options.SerializerSettings.Converters.Add(new FirewallRuleConverter());
	options.SerializerSettings.Converters.Add(new GuestMachineIdConverter());
});

builder.Services
	.AddLogging(s => s.AddConsole().SetMinimumLevel(LogLevel.Trace))

	.AddSingleton(new ProxmoxBasedConfigurationProvider.Options())
	.AddTransient<IServiceConfigurationProvider, ProxmoxBasedConfigurationProvider>()

	.AddSingleton(new GlobalConfigurationProvider.Options())
	.AddTransient<IServiceConfigurationProvider, GlobalConfigurationProvider>()
;

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

var configBuilder = new ServiceConfigurationBuilder();
foreach (var provider in app.Services.GetServices<IServiceConfigurationProvider>())
	await provider.FetchConfigurationAsync(configBuilder);

var configuration = configBuilder.Build();

logger.LogInformation("Using configuration: DNSZones=[{DNSZones}], FallbackDNSServer={FallbackDNSServer}, MainInterface={MainInterface}, ServerName={ServerName}",
	configuration.DNSZones, configuration.FallbackDNSServer, configuration.MainInterface?.Name, configuration.ServerName);
foreach (var gm in configuration.Machines)
	logger.LogInformation("Using guest machine configuration: Id={Id}, Name={Name}, MIPA={MIPA}, Rules={RulesCount}",
		gm.Id, gm.Name, gm.NetworkConfiguration.MainInterfacePhysicalAddress, gm.NetworkConfiguration.Rules.Count);

app.MapGet("/", () => JsonConvert.SerializeObject(configuration,
	[new StringEnumConverter(), new ServiceConfigurationConverter(), new FirewallRuleConverter(), new GuestMachineIdConverter()]));

app.Run();

class ServiceConfigurationConverter : JsonConverter<ServiceConfiguration>
{
	public override ServiceConfiguration? ReadJson(JsonReader reader, Type objectType, ServiceConfiguration? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override void WriteJson(JsonWriter writer, ServiceConfiguration? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		serializer.Serialize(writer, new 
		{
			Machines = value.Machines.ToDictionary(s => s.Id, s => new
			{
				s.Name,
				NetworkConfiguration = new
				{
					MainInterfacePhysicalAddress = s.NetworkConfiguration.MainInterfacePhysicalAddress.ToString(),
					s.NetworkConfiguration.Rules
				}
			}),
			value.DNSZones,
			FallbackDNSServer = value.FallbackDNSServer.ToString(),
			MainInterface = value.MainInterface?.Name,
			value.ServerName
		});
	}
}

class FirewallRuleConverter : JsonConverter<FirewallRule>
{
	public override FirewallRule? ReadJson(JsonReader reader, Type objectType, FirewallRule? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override void WriteJson(JsonWriter writer, FirewallRule? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		serializer.Serialize(writer, new
		{
			value.Type,
			value.SourcePort,
			value.DestinationPort,
			value.SourceMachineId,
			value.DestinationMachineId,
			Protocol = (byte)value.Protocol
		});
	}
}

class GuestMachineIdConverter : JsonConverter<GuestMachineId>
{
	public override GuestMachineId ReadJson(JsonReader reader, Type objectType, GuestMachineId existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override void WriteJson(JsonWriter writer, GuestMachineId value, JsonSerializer serializer)
	{
		writer.WriteValue(value.Id);
	}
}
