using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.JsonSerialization;
using GLNetTools.NetworkConfigurationService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

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

	.AddTransient<IJsonServiceConfigurationSerializer, JsonServiceConfigurationSerializer>()
	.AddSingleton(new ConfigurationLoader.Options() { ConfigurationServiceURL = new Uri(args[0]) })
	.AddSingleton<ConfigurationLoader>()

	.AddSingleton<INetworkService, DomainNameSystemService>()
	.AddSingleton<INetworkService, DynamicHostConfigurationProtocolService>()

	.BuildServiceProvider();

var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
var configurationLoader = services.GetRequiredService<ConfigurationLoader>();
var networkServices = services.GetServices<INetworkService>().ToHashSet();
var globalCTS = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) => { logger.LogInformation("SIGINT detected. Stopping..."); globalCTS.Cancel(); };

HashSet<string> scopes = [], modules = [];
foreach (var service in networkServices)
{
	service.GetConfigurationQueryOptions(out var serviceScopes, out var serviceModules);
	foreach (var item in serviceScopes) scopes.Add(item);
	foreach (var item in serviceModules) modules.Add(item);
}

var startedServices = new HashSet<INetworkService>();

try
{
	ServiceConfiguration actualConfiguration;
	var updates = configurationLoader.LoadConfigurationAsync(scopes, modules, globalCTS.Token).GetAsyncEnumerator();

	while (true) // While `actualConfiguration` is not initialized
	{
		await updates.MoveNextAsync();
		bool criticallyStarted = false;

		if (updates.Current is ErroredConfigurationUpdateEvent error)
		{
			logFetchError(error);
			logger.LogInformation("No configuration loaded at startup, starting services in anti-crisis critical mode");
			if (criticallyStarted == false)
			{
				foreach (var service in networkServices)
				{
					try
					{
						service.StartCritical();
						logger.LogDebug("Service {Service} started in critical mode", service);
						startedServices.Add(service);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error during service {Service} critical starting", service);
					}
				}

				criticallyStarted = true;
			}
		}
		else if (updates.Current is SuccessfulConfigurationUpdateEvent success)
		{
			logger.LogInformation("First version of configuration loaded");
			actualConfiguration = success.NewConfiguration;
			printConfiguration(actualConfiguration);
			break;
		}
		else throw new NotSupportedException();
	}


	while (true) // Until exit signal
	{
		foreach (var service in startedServices.ToArray())
		{
			try
			{
				service.Stop();
				logger.LogDebug("Service {Service} stopped", service);
				startedServices.Remove(service);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during service {Service} stopping", service);
			}
		}

		foreach (var service in networkServices.Except(startedServices))
		{
			try
			{
				service.Start(actualConfiguration);
				logger.LogDebug("Service {Service} started", service);
				startedServices.Add(service);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during service {Service} starting", service);
			}
		}

		while (true) // While `actualConfiguration` is not changed
		{
			await updates.MoveNextAsync();
			if (updates.Current is ErroredConfigurationUpdateEvent error)
			{
				logFetchError(error);
				continue;
			}
			else if (updates.Current is SuccessfulConfigurationUpdateEvent success)
			{
				logger.LogInformation("New configuration loaded");
				actualConfiguration = success.NewConfiguration;
				printConfiguration(actualConfiguration);
				break;
			}
			else throw new NotSupportedException();
		}

	}
}
catch (TaskCanceledException)
{
	logger.LogInformation("Stop signal received, performing exit");
}
catch (Exception ex)
{
	logger.LogCritical(ex, "Critical error in main cycle, exiting...");
}


foreach (var service in startedServices)
{
	try
	{
		service.Stop();
		logger.LogDebug("Service {Service} stopped", service);
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Error during service {Service} stopping", service);
	}
}

logger.LogInformation("Done.");



void logFetchError(ErroredConfigurationUpdateEvent update)
{
	logger.LogError(update.Exception, "Unexcepted error during fetching new configuration");
}

void printConfiguration(ServiceConfiguration configuration)
{
	var builder = new StringBuilder();
	foreach (var scopeType in configuration.ScopeTypes)
	{
		builder.AppendLine($"\tScopeType{{Name={scopeType.Name}, KeyType={scopeType.KeyType.FullName}}}:");
		foreach (var scope in configuration.FilterScopes(scopeType))
		{
			builder.AppendLine($"\t\tScope{{Key={scope.WeakKey}}}:");
			foreach (var projection in scope.Projections)
			{
				builder.AppendLine($"\t\t\tProjection{{Prototype.Module.Name={projection.Key}}}:");
				foreach (var property in projection.Value.Data)
				{
					builder.AppendLine($"\t\t\t\t[{property.Key}] <=> Property{{Type={property.Value.Property.Type.FullName}}} <=> [{property.Value.Value}]");
				}
			}
		}
	}

	logger.LogDebug("Using configuration: \n{ConfigurationDump}", builder.ToString());
}