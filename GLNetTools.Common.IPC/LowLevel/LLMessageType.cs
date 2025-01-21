namespace GLNetTools.Common.IPC.LowLevel
{
    public enum LLMessageType : byte
    {
        Handshake = 0,
        DropConnection = 1,
        Heartbeat = 2,

        Event = 11,
        DataRequest = 12,
        DataReply = 13,
        PerformOperationRequest = 14,
        RemoteOperationResult = 15
    }
}
