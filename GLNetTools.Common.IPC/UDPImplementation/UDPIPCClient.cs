using System.Net;
using System.Net.Sockets;
using GLNetTools.Common.IPC.LowLevel;

namespace GLNetTools.Common.IPC.UDPImplementation
{
    public class UDPIPCClient : IIPCClient
	{
		public const int LocalPort = 99;


		private readonly Socket _udp;
		private readonly ICommunicationAgent _agent;
		private readonly Thread _receiveThread;
		private IIPCChannelHandler? _channelHandler;
		private IPEndPoint? _remoteEndPoint;
		private bool _isRemoteAlive;
		private bool _isConnected = false;


		public UDPIPCClient(ICommunicationAgent agent)
		{
			_udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_receiveThread = new Thread(ReceiveThreadHandler);
			_agent = agent;
		}


		public bool IsRemoteAlive { get { ThrowUnlessConnected(); return _isRemoteAlive; } }

		public IPEndPoint RemoteEndPoint { get { ThrowUnlessConnected(); return _remoteEndPoint!; } }

		EndPoint IIPCChannel.RemoteEndPoint => RemoteEndPoint;

		private bool IsConnected => _remoteEndPoint is not null;


		public void Connect(IPEndPoint endPoint)
		{
			_udp.Bind(new IPEndPoint(IPAddress.Any, LocalPort));

			_remoteEndPoint = endPoint;
			SendLLMessage(_agent.CreateHandshake());

			var handshakeMessage = ReceiveLLMessage();
			if (_agent.CheckHandshake(handshakeMessage) == false)
			{
				SendLLMessage(_agent.CreateDropConnection());
				throw new Exception("WTF"); //TODO: throw normal exception
			}

			_isRemoteAlive = true;
			_isConnected = true;
			_receiveThread.Start();
		}

		public void Disconnect()
		{
			SendLLMessage(_agent.CreateDropConnection());
			_isConnected = false;
			_receiveThread.Join();
			_udp.Close();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_udp.Dispose();
		}

		public ValueTask<RemoteOperationResult> PerformOperation(string operationPrompt, PropertyObject arguments)
		{
			throw new NotImplementedException();
		}

		public ValueTask<PropertyObject> RequestAsync(string objectName)
		{
			throw new NotImplementedException();
		}

		public void SendEventUpdateMessage(string objectName, PropertyObject updatedValues)
		{
			SendLLMessage(_agent.CreateEventUpdateMessage(objectName, updatedValues));
		}

		public void SetChannelHandler(IIPCChannelHandler handler)
		{
			if (_channelHandler is not null)
				throw new InvalidOperationException("Handler already assigned");
			_channelHandler = handler;
		}

		private void SendLLMessage(LowLevelMessage message)
		{
			Span<byte> bytes = stackalloc byte[message.Bytes.Length + 1];
			bytes[0] = (byte)message.Type;
			message.Bytes.CopyTo(bytes[1..]);

			_udp.SendTo(bytes, _remoteEndPoint ?? throw new NullReferenceException());
		}

		private LowLevelMessage ReceiveLLMessage()
		{
		dropMessage:
			IPEndPoint receivedEndPoint = _remoteEndPoint ?? throw new NullReferenceException();
			var received = _udp.Receive(ref receivedEndPoint).AsSpan();
			if (Equals(receivedEndPoint, _remoteEndPoint) == false)
				goto dropMessage;

			var type = (LLMessageType)received[0];
			var bytes = received[1..];
			return new LowLevelMessage(type, bytes);
		}

		private void ReceiveThreadHandler()
		{
			_udp.ReceiveTimeout = 1000;
			EndPoint rep = new IPEndPoint(IPAddress.Any, LocalPort);
			var buffer = new byte[10241];

			while (_isConnected)
			{
				try
				{
					_udp.ReceiveFrom(buffer, ref rep);
					if (Equals(rep, RemoteEndPoint) == false)
						continue;


				}
				catch (SocketException)
				{

				}
			}
		}

		private void ThrowUnlessConnected()
		{
			if (IsConnected)
				throw new InvalidOperationException("Connect client first");
		}
	}
}
