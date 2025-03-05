using DNS.Protocol;
using DNS.Server;
using System.Net;
using System.Net.Sockets;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DomainNameSystemService : INetworkService
	{
		private DnsServer? _server = null;


		public void Setup(ServiceConfiguration configuration)
		{
			var ip = configuration.MainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);

			var masterFile = new MasterFile();
			_server = new DnsServer(masterFile, configuration.FallbackDNSServer);
			_server.Responded += (s, e) => Console.WriteLine($"""
			[DNS] request from {e.Remote} for {string.Join(",", e.Request.Questions.Select(s => s.Name))} satisfied
			""");

			Span<byte> address = stackalloc byte[4];
			foreach (var gm in configuration.Machines)
			{
				ip.Address.TryWriteBytes(address, out _);
				if (gm.Id > byte.MaxValue)
					throw new Exception($"Invalid configuration -> GM id too big ({gm.Name}[{gm.Id}])");
				address[3] = (byte)gm.Id;
				masterFile.AddIPAddressResourceRecord(new Domain(gm.Name + "." + configuration.DNSZone), new IPAddress(address));
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
