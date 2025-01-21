using System.Net;

namespace GLNetTools.Common.IPC
{
	public interface IIPCChannel
	{
		public bool IsRemoteAlive { get; }

		public EndPoint RemoteEndPoint { get; }


		public void SendEventUpdateMessage(string objectName, PropertyObject updatedValues);

		public ValueTask<PropertyObject> RequestAsync(string objectName);

		public ValueTask<RemoteOperationResult> PerformOperation(string operationPrompt, PropertyObject arguments);

		public void SetChannelHandler(IIPCChannelHandler handler);
	}
}
