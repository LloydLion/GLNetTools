using DNS.Protocol;
using DotNetProjects.DhcpServer;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DynamicHostConfigurationProtocolService : INetworkService
	{
		private DHCPServer? _server;


		public void Setup(ServiceConfiguration configuration)
		{
			_server = new DHCPServer
			{
				ServerName = configuration.ServerName,
				BroadcastAddress = IPAddress.Broadcast,
				SendDhcpAnswerNetworkInterface = configuration.MainInterface
			};

			_server.OnDataReceived += (req) => Request(req, configuration);
		}

		public void Start()
		{

			if (_server is null)
				throw new InvalidOperationException("Setup service first");
			_server.Start();
		}

		static void Request(DHCPRequest request, ServiceConfiguration configuration)
		{
			try
			{
				var ip = configuration.MainInterface.GetIPProperties().UnicastAddresses.First(s => s.Address.AddressFamily == AddressFamily.InterNetwork);

				var type = request.GetMsgType();
				var mac = new PhysicalAddress(request.GetChaddr());

				GuestMachineConfiguration? target = null;

				foreach (var gm in configuration.Machines)
				{
					if (gm.MainInterfacePhysicalAddress.Equals(mac))
					{
						target = gm;
						break;
					}
				}
				if (target is null)
					return;

				var replyOptions = new DHCPReplyOptions
				{
					SubnetMask = IPAddress.Parse("255.255.255.0"),
					DomainName = configuration.ServerName,
					ServerIdentifier = ip.Address,
					RouterIP = ip.Address,
					DomainNameServers = [ip.Address],
					IPAddressLeaseTime = uint.MaxValue
				};

				Span<byte> address = stackalloc byte[4];
				ip.Address.TryWriteBytes(address, out _);
				var targetIP = new IPAddress(address);

				if (type == DHCPMsgType.DHCPDISCOVER)
					request.SendDHCPReply(DHCPMsgType.DHCPOFFER, targetIP, replyOptions);
				else if (type == DHCPMsgType.DHCPREQUEST)
					request.SendDHCPReply(DHCPMsgType.DHCPACK, targetIP, replyOptions);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}
