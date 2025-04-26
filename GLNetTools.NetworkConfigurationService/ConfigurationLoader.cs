using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.JsonSerialization;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace GLNetTools.NetworkConfigurationService
{
	internal class ConfigurationLoader
	{
		private readonly Options _options;
		private readonly IJsonServiceConfigurationSerializer _serializer;


		public ConfigurationLoader(Options options, IJsonServiceConfigurationSerializer serializer)
		{
			_options = options;
			_serializer = serializer;
		}


		public async IAsyncEnumerable<ConfigurationUpdateEvent> LoadConfigurationAsync(IReadOnlyCollection<string> scopes, IReadOnlyCollection<string> modules, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var client = new HttpClient() { Timeout = TimeSpan.FromMinutes(2) };
			DateTime? waitForOlderThan = null;

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				ConfigurationUpdateEvent update;
				HttpResponseMessage? response = null;
				try
				{
				timeoutRetry:
					try
					{
						var query = new { Scopes = scopes, Modules = modules, WaitForOlderThan = waitForOlderThan };

						var request = new HttpRequestMessage
						{
							Method = HttpMethod.Get,
							Content = new StringContent(JsonConvert.SerializeObject(query)),
							RequestUri = _options.ConfigurationServiceURL
						};

						response = await client.SendAsync(request, cancellationToken);
					}
					catch (TaskCanceledException)
					{
						if (cancellationToken.IsCancellationRequested)
							throw;
						goto timeoutRetry;
					}

					response.EnsureSuccessStatusCode();

					var content = await response.Content.ReadAsStringAsync(cancellationToken);
					var configuration = _serializer.Deserialize(content);
					waitForOlderThan = configuration.Version;

					update = new SuccessfulConfigurationUpdateEvent
					{
						RawResponse = response,
						NewConfiguration = configuration
					};

				}
				catch (Exception ex)
				{
					update = new ErroredConfigurationUpdateEvent
					{
						MaybeRawResponse = response,
						Exception = ex
					};
				}

				yield return update;

				if (update is ErroredConfigurationUpdateEvent)
					await Task.Delay(_options.ErrorTimeout, cancellationToken);
			}
		}


		public class Options
		{
			public TimeSpan LongPoolTimeout { get; init; } = TimeSpan.FromMinutes(2);

			public TimeSpan ErrorTimeout { get; init; } = TimeSpan.FromMinutes(1);

			public required Uri ConfigurationServiceURL { get; init; }
		}
	}

	public abstract class ConfigurationUpdateEvent
	{
		public HttpResponseMessage? MaybeRawResponse { get; init; }
	}

	public sealed class ErroredConfigurationUpdateEvent : ConfigurationUpdateEvent
	{
		public required Exception Exception { get; init; }
	}

	public sealed class SuccessfulConfigurationUpdateEvent : ConfigurationUpdateEvent
	{
		public required HttpResponseMessage RawResponse { get => MaybeRawResponse ?? throw new NullReferenceException(); init => MaybeRawResponse = value; }

		public required ServiceConfiguration NewConfiguration { get; init; }
	}
}
