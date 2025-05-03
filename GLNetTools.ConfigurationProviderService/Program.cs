global using ConfigurationBuilder = GLNetTools.Common.Configuration.ConfigurationBuilder;
global using IConfigurationBuilder = GLNetTools.Common.Configuration.IConfigurationBuilder;
global using IConfigurationProvider = GLNetTools.Common.Configuration.IConfigurationProvider;
using Newtonsoft.Json;
using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.BuiltIn;
using GLNetTools.ConfigurationProviderService.Providers;
using GLNetTools.Common.Configuration.JsonSerialization;

namespace GLNetTools.ConfigurationProviderService;

internal class Program
{
	private static async Task Main(string[] args)
	{
#if DEBUG
		if (args.Contains("--no-dbg") == false)
		{
			Console.WriteLine("Waiting for debugger to attach");
			while (!System.Diagnostics.Debugger.IsAttached)
				Thread.Yield();
			Console.WriteLine("Debugger attached");
		}
#endif

		var webBuilder = WebApplication.CreateBuilder(args);

		webBuilder.Services
			.AddLogging(s => s.AddConsole().SetMinimumLevel(LogLevel.Trace))

			.AddSingleton(new ProxmoxBasedConfigurationProvider.Options())
			.AddTransient<IConfigurationProvider, ProxmoxBasedConfigurationProvider>()

			.AddSingleton(new GlobalConfigurationProvider.Options())
			.AddTransient<IConfigurationProvider, GlobalConfigurationProvider>()

			.AddSingleton<IConfigurationBuilder, ConfigurationBuilder>()
			.AddSingleton<ConfigurationHolder>()
			.AddSingleton<ConfigurationProviderDispatcher>()

			.AddSingleton<ConfigurationScopeTypeRegistry>()
			.AddSingleton<ConfigurationModuleRegistry>()

			.AddTransient(sp => JSONNetConverters.CreateSerializer())
			.AddSingleton<IJsonServiceConfigurationSerializer, JsonServiceConfigurationSerializer>()

			.AddTransient<WebController>()
		;

		var app = webBuilder.Build();
		var logger = app.Services.GetRequiredService<ILogger<Program>>();
		logger.LogInformation("Application built");

		var scopeRegistry = app.Services.GetRequiredService<ConfigurationScopeTypeRegistry>();
		scopeRegistry.Register(BuiltInScopeTypes.Master);
		scopeRegistry.Register(BuiltInScopeTypes.GuestMachine);
		logger.LogTrace("ConfigurationScopeType registration complete");

		var moduleRegistry = app.Services.GetRequiredService<ConfigurationModuleRegistry>();
		moduleRegistry.Register(BaseModule.Instance);
		moduleRegistry.Register(NetworkModule.Instance);
		logger.LogTrace("ConfigurationModule registration complete");

		var holder = app.Services.GetRequiredService<ConfigurationHolder>();
		var dispatcher = app.Services.GetRequiredService<ConfigurationProviderDispatcher>();

		await dispatcher.PerformInitialConfigurationLoadAsync(holder);
		holder.RebuildConfiguration();
		logger.LogInformation("Initial config load complete");

		dispatcher.InitializeTracking(holder);
		dispatcher.EnableTracking();
		logger.LogInformation("Config tracking initialized and enabled");

		logger.LogInformation("Application is starting...");

		Console.WriteLine("Configuration:");
		foreach (var scopeType in holder.Configuration.ScopeTypes)
		{
			Console.WriteLine($"\tScopeType{{Name={scopeType.Name}, KeyType={scopeType.KeyType.FullName}}}:");
			foreach (var scope in holder.Configuration.FilterScopes(scopeType))
			{
				Console.WriteLine($"\t\tScope{{Key={scope.WeakKey}}}:");
				foreach (var projection in scope.Projections)
				{
					Console.WriteLine($"\t\t\tProjection{{Prototype.Module.Name={projection.Key}}}:");
					foreach (var property in projection.Value.Data)
					{
						Console.WriteLine($"\t\t\t\t[{property.Key}] <=> Property{{Type={property.Value.Property.Type.FullName}}} <=> [{property.Value.Value}]");
					}
				}
			}
		}

		app.MapGet("/fetch", app.Services.GetRequiredService<WebController>().HandleConfigurationRequest);

		app.Run();
	}
}
