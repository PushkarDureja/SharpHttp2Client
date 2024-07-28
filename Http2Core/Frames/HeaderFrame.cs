namespace Http2Core.Frames
{
    public class HeaderFrame : Frame
    {
        private byte _padLength;
        private byte _priorityWeight;
        private int _dependentStreamId;
        private List<byte> _headerBlockFragment;

        public HeaderFrame(int length, FrameType type, byte flags, int streamId, byte[] payload) : base(length, type, flags, streamId, payload)
        {
        }

        protected override async Task ParsePayload()
        {
            using MemoryStream stream = new(Payload);
            stream.Position = 0;

            int bytesRead = 0;
            int totalBytesRead = 0;
            byte[] payloadBuffer = new byte[Payload.Length];

            if (Flags.Contains(FrameFlags.Padded))
            {
                bytesRead = await stream.ReadAsync(payloadBuffer.AsMemory(0, 1));
                if (bytesRead < 1)
                    throw new NotSupportedException("Invalid Frame Payload, Missing PadLength");

                _padLength = payloadBuffer[0];
                totalBytesRead += bytesRead;
            }

            if (Flags.Contains(FrameFlags.Priority))
            {
                bytesRead = await stream.ReadAsync(payloadBuffer.AsMemory(0, 5));
                if (bytesRead < 1)
                    throw new NotSupportedException("Invalid Frame Payload, Missing Priority Flag related fields");

                _dependentStreamId = payloadBuffer[3] | payloadBuffer[2] << 8 | payloadBuffer[1] << 16;
                _priorityWeight = payloadBuffer[4];
                totalBytesRead += bytesRead;
            }

            int headerBlockSize = Payload.Length - totalBytesRead - _padLength;
            _headerBlockFragment = new(headerBlockSize);

            bytesRead = await stream.ReadAsync(payloadBuffer.AsMemory(0, headerBlockSize));
            if (bytesRead < 1)
                throw new NotSupportedException("Invalid Frame Payload, Missing Header Block Fragment");

            _headerBlockFragment.AddRange(payloadBuffer.Take(headerBlockSize));
        }

        public byte PadLength => _padLength;

        public byte PriorityWeight => _priorityWeight;

        public int DependentStreamId => _dependentStreamId;

        public IReadOnlyCollection<byte> HeaderBlockFragment => _headerBlockFragment;
    }
}
