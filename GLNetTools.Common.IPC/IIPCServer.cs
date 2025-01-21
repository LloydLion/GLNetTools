using System.Net;

namespace GLNetTools.Common.IPC
{
	public interface IIPCServer : IDisposable
	{
		public void Bind(EndPoint localEndPoint);

		public IIPCChannel AcceptNewClient();

		public void CloseAllClients();
	}
}
