namespace GLNetTools.Common.IPC.LowLevel
{
    public readonly ref struct LowLevelMessage()
    {
        public LowLevelMessage(LLMessageType type, ReadOnlySpan<byte> bytes) : this()
        {
            Type = type;
            Bytes = bytes;
        }


        public LLMessageType Type { get; }

        public ReadOnlySpan<byte> Bytes { get; }
    }
}
