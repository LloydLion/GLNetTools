using GitHub.JPMikkers.DHCP;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DynamicHostConfigurationProtocolService : INetworkService
	{
		private DHCPServer? _server;
		private readonly ILogger<DynamicHostConfigurationProtocolService> _logger;


		public DynamicHostConfigurationProtocolService(ILogger<DynamicHostConfigurationProtocolService> logger)
		{
			_logger = logger;
		}


		public bool Setup(ServiceConfiguration configuration)
		{
			if (configuration.MainInterface is null)
			{
				_logger.LogCritical("DHCP service cannot setup in critical mode");
				return false;
			}

			var ip = configuration.MainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);
			_logger.LogDebug("Using {IPAddress}:67 for DHCP server", ip.Address);

			Span<byte> address = stackalloc byte[4];
			ip.Address.TryWriteBytes(address, out _);

			address[3] = 200;
			var poolStart = new IPAddress(address);
			address[3] = 250;
			var poolEnd = new IPAddress(address);
						
			_server = new DHCPServer(_logger, new DefaultUDPSocketFactory(_logger))
			{
				EndPoint = new IPEndPoint(ip.Address, 67),
				LeaseTime = TimeSpan.MaxValue,
				SubnetMask = IPAddress.Parse("255.255.255.0"),
				PoolStart = poolStart,
				PoolEnd = poolEnd,
				OfferExpirationTime = TimeSpan.FromSeconds(30),
				MinimumPacketSize = 576
			};

			var options = new List<OptionItem>()
			{
				new(OptionMode.Default, new DHCPOptionRouter() { IPAddresses = [ip.Address] }),
				new(OptionMode.Default, new DHCPOptionDomainNameServer() { IPAddresses = [ip.Address] })
			};
			_server.Options = options;

			var reservations = new List<ReservationItem>();
			foreach (var gm in configuration.Machines)
			{
				address[3] = gm.Id;
				var targetIP = new IPAddress(address);

				reservations.Add(new ReservationItem()
				{
					MacTaste = gm.MainInterfacePhysicalAddress.ToString(),
					PoolStart = targetIP,
					PoolEnd = targetIP,
					Preempt = true
				});
				_logger.LogDebug("Added DHCP reservation for {MIPA} (GM={ID}) <=> {IPAddress}", gm.MainInterfacePhysicalAddress, gm.Id, targetIP);
			}

			_server.Reservations = reservations;
			return true;
		}

		public void Start()
		{
			if (_server is null)
				throw new InvalidOperationException("Setup service first");
			_server.Start();
		}
	}
}