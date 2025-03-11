using DNS.Protocol;
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

			var masterFile = new MasterFile();
			_server = new DnsServer(masterFile, configuration.FallbackDNSServer);
			_server.Responded += logResponded;

			Span<byte> address = stackalloc byte[4];
			ip.Address.TryWriteBytes(address, out _);

			var zones = configuration.DNSZones.Split(";");
			foreach (var gm in configuration.Machines)
			{
				address[3] = gm.Id;
				var ipAddress = new IPAddress(address);
				
				foreach (var zone in zones)
				{
					try
					{
						var domain = new Domain(gm.Name + "." + configuration.DNSZones);
						masterFile.AddIPAddressResourceRecord(domain, ipAddress);
						_logger.LogDebug("A {Domain} -> {Address} added to master file", domain, ipAddress);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Failed to add DNS record for GM {Id} in {Zone} zone (IPAddress={IPAddress}). Record skipped", gm.Id, zone, ipAddress);
					}
				}
				_logger.LogTrace("GM {Id} finished to be processed", gm.Id);
			}

			return true;



			[SuppressMessage("Usage", "CA2254")]
			void logResponded(object? sender, DnsServer.RespondedEventArgs args)
			{
				var questions = string.Join(", ", args.Request.Questions.Select(s => s.Type.ToString() + "=" + s.Name.ToString()));
				var answers = string.Join(", ", args.Response.AnswerRecords);

				_logger.LogDebug("Request from {RemoteEndPoint} for [{Questions}] satisfied with [{Answers}]", args.Remote, questions, answers);
			}
		}

		public void Start()
		{
			if (_server is null)
				throw new InvalidOperationException("Setup service first");
			_server.Listen();
		}
	}
}
