namespace GLNetTools.Common.IPC
{
	public interface IIPCChannelHandler
	{
		public ValueTask HandleUpdateEventAsync(string objectName, PropertyObject updatedValues);

		public ValueTask<PropertyObject> HandleRequestAsync(string objectName);

		public ValueTask<RemoteOperationResult> HandleOperationAsync(string operationPrompt, PropertyObject arguments);
	}
}