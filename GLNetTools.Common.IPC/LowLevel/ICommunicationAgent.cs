
namespace GLNetTools.Common.IPC.LowLevel
{
	public interface ICommunicationAgent
	{
		public string ProtocolName { get; }

		public int ProtocolVersion { get; }


		public LowLevelMessage CreateHandshake();

		public bool CheckHandshake(LowLevelMessage message);

		public LowLevelMessage CreateDropConnection();

		public LowLevelMessage CreateEventUpdateMessage(string objectName, PropertyObject updatedValues);

		public bool ShouldDisconnectByTimeout(DateTime lastMessage);
	}
}
