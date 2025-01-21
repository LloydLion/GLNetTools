using System.Net;

namespace GLNetTools.Common.IPC
{
	public interface IIPCClient : IIPCChannel, IDisposable
	{
		public new IPEndPoint RemoteEndPoint { get; }


		public void Connect(IPEndPoint endPoint);

		public void Disconnect();
	}
}
