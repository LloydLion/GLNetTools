using DNS.Protocol;
using DotNetProjects.DhcpServer;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace GLNetTools.NetworkConfigurationService
{
	internal class DynamicHostConfigurationProtocolService : INetworkService
	{
		private DHCPServerFixed? _server;


		public void Setup(ServiceConfiguration configuration)
		{
			_server = new DHCPServerFixed
			{
				ServerName = configuration.ServerName,
				BroadcastAddress = IPAddress.Broadcast,
				SendDhcpAnswerNetworkInterface = configuration.MainInterface
			};

			_server.UnhandledException += Console.WriteLine;
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
				address[3] = (byte)target.Id;
				var targetIP = new IPAddress(address);

				var replyType = type switch
				{
					DHCPMsgType.DHCPDISCOVER => DHCPMsgType.DHCPOFFER,
					DHCPMsgType.DHCPREQUEST => request.GetRequestedIP().Equals(targetIP) ? DHCPMsgType.DHCPACK : DHCPMsgType.DHCPNAK,
					_ => (DHCPMsgType)0,
				};
				
				request.SendDHCPReply(replyType, targetIP, replyOptions);

				Console.WriteLine($"""
				[DHCP] request {type} from {target.Name}[{target.Id}] satisfied with {replyType} and ip {targetIP}
				""");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}

#nullable disable
namespace DotNetProjects.DhcpServer
{
	public class DHCPServerFixed : DHCPServer, IDisposable
	{
		public DHCPServerFixed(IPAddress bindIp) : base(bindIp) { }

		public DHCPServerFixed() { }

		public new void Start()
		{
			var type = typeof(DHCPServer);

			var _bindIp = (IPAddress)type.GetField("_bindIp", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

			var localEP = new IPEndPoint(_bindIp, 67);
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
			{
				EnableBroadcast = true,
				SendBufferSize = 65536,
				ReceiveBufferSize = 65536
			};
			type.GetField("socket", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(this, socket);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			socket.Bind(localEP);

			//const uint IOC_IN = 0x80000000;
			//const uint IOC_VENDOR = 0x18000000;
			//const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
			//socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null);

			var _cancellationTokenSource = new CancellationTokenSource();
			type.GetField("_cancellationTokenSource", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(this, _cancellationTokenSource);

			var ReceiveDataThread = type.GetMethod("ReceiveDataThread", BindingFlags.NonPublic | BindingFlags.Instance)
				.CreateDelegate<Action>(this);
			var receiveDataTask = new Task(ReceiveDataThread, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
			type.GetField("receiveDataTask", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(this, receiveDataTask);

			receiveDataTask.Start();
		}
	}
}