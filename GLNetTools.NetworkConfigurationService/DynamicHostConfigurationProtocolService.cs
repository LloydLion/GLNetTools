using GitHub.JPMikkers.DHCP;
using GLNetTools.Common.Configuration;
using GLNetTools.Common.Configuration.BuiltIn;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
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


		public void Start(ServiceConfiguration configuration)
		{
			var masterScope = configuration.GetScope(BuiltInScopeTypes.Master, NoScopeKey.Instance);
			var masterNetworkConfig = masterScope.GetProjection(NetworkModule.MasterPrototype);

			var mainInterface = NetworkInterface.GetAllNetworkInterfaces().Single(s => s.Name == masterNetworkConfig.MainInterface);
			var ip = mainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);

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
			foreach (var gm in configuration.FilterScopes(BuiltInScopeTypes.GuestMachine).Select(s => new { s.Key.Id, s.GetProjection(NetworkModule.GuestMachinePrototype).MainInterfacePhysicalAddress }))
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

			_server.Start();
		}


		public void GetConfigurationQueryOptions(out string[] scopes, out string[] modules)
		{
			scopes = [BuiltInScopeTypes.Master.Name, BuiltInScopeTypes.GuestMachine.Name];
			modules = [NetworkModule.Instance.Name];
		}

		public void StartCritical()
		{
			throw new NotSupportedException();
		}

		public void Restart(ServiceConfiguration newConfiguration)
		{
			Stop();
			Start(newConfiguration);
		}

		public void Stop()
		{
			_server!.Stop();
		}
	}
}