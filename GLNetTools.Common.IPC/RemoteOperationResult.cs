namespace GLNetTools.Common.IPC
{
	public class RemoteOperationResult
	{
		public Guid LogEntryId { get; init; } = Guid.Empty;

		public string? TextMessage { get; init; } = null;

		public required bool IsSuccess { get; init; }
	}
}
