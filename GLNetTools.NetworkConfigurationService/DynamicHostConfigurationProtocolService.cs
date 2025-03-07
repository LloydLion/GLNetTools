using GitHub.JPMikkers.DHCP;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DynamicHostConfigurationProtocolService : INetworkService
	{
		private DHCPServer? _server;


		public void Setup(ServiceConfiguration configuration)
		{
			var ip = configuration.MainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);
			Span<byte> address = stackalloc byte[4];
			ip.Address.TryWriteBytes(address, out _);

			address[3] = 200;
			var poolStart = new IPAddress(address);
			address[3] = 250;
			var poolEnd = new IPAddress(address);

			File.Delete("dhcp_clients.xml");
			
			_server = new DHCPServer(new StubLogger("server"), new DefaultUDPSocketFactory(new StubLogger("sockets")))

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
				address[3] = (byte)gm.Id;
				var targetIP = new IPAddress(address);

				reservations.Add(new ReservationItem()
				{
					MacTaste = gm.MainInterfacePhysicalAddress.ToString(),
					PoolStart = targetIP,
					PoolEnd = targetIP,
					Preempt = true
				});
			}
			_server.Reservations = reservations;

			_server.OnStatusChange += (s, e) => Console.WriteLine(e?.Reason);
		}

		public void Start()
		{
			if (_server is null)
				throw new InvalidOperationException("Setup service first");
			_server.Start();
		}


		private class StubLogger(string category) : ILogger
		{
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			{
				return null;
			}

			public bool IsEnabled(LogLevel logLevel)
			{
				return true;
			}

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			{
				Console.WriteLine($"DHCP [{category}]: ");
				Console.WriteLine(formatter(state, exception));
			}
		}
	}
}