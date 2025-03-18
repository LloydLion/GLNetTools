using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using DNS.Server;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
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


		public bool Setup(ServiceConfiguration configuration)
		{
			var mainInterface = configuration.MainInterface;
			if (mainInterface is null)
			{
				_logger.LogWarning("Running in critical mode, local GM addresses will not be resolved");
				_server = new DnsServer(configuration.FallbackDNSServer);
				_server.Responded += logResponded;
				return true;
			}
			
			var ip = mainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);

			var masterFile = new ConfigurationBasedDNSResolver(configuration.FallbackDNSServer);
			_server = new DnsServer(masterFile);
			_server.Responded += logResponded;

			Span<byte> address = stackalloc byte[4];
			ip.Address.TryWriteBytes(address, out _);

			foreach (var gm in configuration.Machines)
			{
				address[3] = gm.Id;
				var ipAddress = new IPAddress(address);
				
				foreach (var zone in configuration.DNSZones)
				{
					try
					{
						var domain = formDomain(zone, gm);
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

			return true;



			void logResponded(object? sender, DnsServer.RespondedEventArgs args)
			{
				var questions = string.Join(", ", args.Request.Questions.Select(s => s.Type.ToString() + "=" + s.Name.ToString()));
				var answers = string.Join(", ", args.Response.AnswerRecords);

				_logger.LogDebug("Request from {RemoteEndPoint} for [{Questions}] satisfied with [{Answers}]", args.Remote, questions, answers);
			}

			Domain formDomain(string template, GuestMachineConfiguration gm)
			{
				template = template.Replace("{Name}", gm.Name);
				template = template.Replace("{Id}", gm.Id.ToString());
				template = template.Replace("{ServerName}", configuration.ServerName);
				return new Domain(template);
			}
		}

		public void Start()
		{
			if (_server is null)
				throw new InvalidOperationException("Setup service first");
			_server.Listen();
		}


		private class ConfigurationBasedDNSResolver : IRequestResolver
		{
			private readonly Dictionary<Domain, IPAddress> _map = new();
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
				foreach (Question question in request.Questions)
				{
					if (_map.TryGetValue(question.Name, out var address))
					{
						if (question.Type == RecordType.A)
							response.AnswerRecords.Add(new IPAddressResourceRecord(question.Name, address));
						else response.ResponseCode = ResponseCode.NameError;

					}
					else
					{
						var fallbackResponse = await _fallbackDNSServer.Resolve(new Request(new Header(), [question], []), cancellationToken);
						if (fallbackResponse.AnswerRecords.Count > 0)
						{
							foreach (var answerRecord in fallbackResponse.AnswerRecords)
								response.AnswerRecords.Add(answerRecord);
						}
						else response.ResponseCode = ResponseCode.NameError;
					}
				}

				return response;
			}
		}
	}
}
