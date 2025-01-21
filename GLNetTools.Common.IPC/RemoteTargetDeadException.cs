namespace GLNetTools.Common.IPC
{
	public class RemoteTargetDeadException : Exception
	{
		public RemoteTargetDeadException() : base("Enable to perform action to dead target")
		{

		}
	}
}
