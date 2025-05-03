using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.BuiltIn;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DomainNameSystemService : INetworkService
	{
		private DnsServer? _server = null;
		private readonly ILogger<DomainNameSystemService> _logger;


		public DomainNameSystemService(ILogger<DomainNameSystemService> logger)
		{
			_logger = logger;
		}


		public void StartCritical()
		{
			IPAddress defaultFallbackDNS = IPAddress.Parse("1.1.1.1");

			_logger.LogWarning("Running in critical mode, local GM addresses will not be resolved");
			_server = new DnsServer(defaultFallbackDNS);
			_server.Responded += LogResponded;
			_server.Errored += LogErrored;

			_server.Listen();
		}

		public void Start(ServiceConfiguration configuration)
		{
			var masterScope = configuration.GetScope(BuiltInScopeTypes.Master, NoScopeKey.Instance);
			var masterNetworkConfig = masterScope.GetProjection(NetworkModule.MasterPrototype);
			var masterBaseConfig = masterScope.GetProjection(BaseModule.MasterPrototype);

			var mainInterface = NetworkInterface.GetAllNetworkInterfaces().Single(s => s.Name == masterNetworkConfig.MainInterface);

			var ip = mainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);

			var masterFile = new ConfigurationBasedDNSResolver(masterNetworkConfig.FallbackDNS);
			_server = new DnsServer(masterFile);
			_server.Responded += LogResponded;
			_server.Errored += LogErrored;

			Span<byte> address = stackalloc byte[4];
			ip.Address.TryWriteBytes(address, out _);


			var machines = configuration.FilterScopes(BuiltInScopeTypes.GuestMachine).Select(s =>
				new { s.Key.Id, Network = s.GetProjection(NetworkModule.GuestMachinePrototype), Base = s.GetProjection(BaseModule.GuestMachinePrototype) });

			foreach (var gm in machines)
			{
				address[3] = gm.Id;
				var ipAddress = new IPAddress(address);

				foreach (var zone in masterNetworkConfig.DNSZones)
				{
					try
					{
						var domain = formDomain(zone, gm.Base.HostName, gm.Id);
						masterFile.Register(domain, ipAddress);
						_logger.LogDebug("A {Domain} -> {Address} added to resolver", domain, ipAddress);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to add DNS record for GM {Id} in {Zone} zone (IPAddress={IPAddress}). Record skipped", gm.Id, zone, ipAddress);
					}
				}
				_logger.LogTrace("GM {Id} finished to be processed", gm.Id);
			}

			_server.Listen();



			Domain formDomain(string template, string hostName, byte id)
			{
				template = template.Replace("{Name}", hostName);
				template = template.Replace("{Id}", id.ToString());
				template = template.Replace("{ServerName}", masterBaseConfig.HostName);
				return new Domain(template);
			}
		}

		public void Restart(ServiceConfiguration newConfiguration)
		{
			Stop();
			Start(newConfiguration);
		}

		public void Stop()
		{
			_server!.Dispose();
		}

		public void GetConfigurationQueryOptions(out string[] scopes, out string[] modules)
		{
			scopes = [BuiltInScopeTypes.Master.Name, BuiltInScopeTypes.GuestMachine.Name];
			modules = [NetworkModule.Instance.Name, BaseModule.Instance.Name];
		}

		private void LogResponded(object? sender, DnsServer.RespondedEventArgs args)
		{
			var questions = string.Join(", ", args.Request.Questions.Select(s => s.Type.ToString() + "=" + s.Name.ToString()));
			var answers = string.Join(", ", args.Response.AnswerRecords);

			_logger.LogDebug("Request from {RemoteEndPoint} for [{Questions}] satisfied with [{Answers}]", args.Remote, questions, answers);
		}

		private void LogErrored(object? sender, DnsServer.ErroredEventArgs args)
		{
			_logger.LogError(args.Exception, "Error during processing request");
		}


		private class ConfigurationBasedDNSResolver : IRequestResolver
		{
			private readonly Dictionary<Domain, IPAddress> _map = [];
			private readonly UdpRequestResolver _fallbackDNSServer;


			public ConfigurationBasedDNSResolver(IPAddress fallbackDNSServer)
			{
				_fallbackDNSServer = new UdpRequestResolver(new IPEndPoint(fallbackDNSServer, 53));
			}


			public void Register(Domain domain, IPAddress ipAddress)
			{
				_map.Add(domain, ipAddress);
			}

			public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
			{
				IResponse response = Response.FromRequest(request);
				bool shouldNotFallback = false;
				foreach (Question question in request.Questions)
				{
					if (_map.TryGetValue(question.Name, out var address))
					{
						if (question.Type == RecordType.A)
							response.AnswerRecords.Add(new IPAddressResourceRecord(question.Name, address));
						else response.ResponseCode = ResponseCode.NameError;
						shouldNotFallback = true;
					}
				}

				if (shouldNotFallback)
					return response;

				response = await _fallbackDNSServer.Resolve(request, cancellationToken);
				return response;
			}
		}
	}
}
