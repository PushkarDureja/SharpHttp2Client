namespace Http2Core.Frames
{
    public class Frame
    {
        private readonly int _length;
        private readonly FrameType _type;
        private readonly byte _rawFlags;
        private readonly int _streamId;
        private readonly byte[] _payload;

        private readonly List<FrameFlags> _flags = [];

        public Frame(int length, FrameType type, byte flags, int streamId, byte[] payload)
        {
            _length = length;
            _type = type;
            _rawFlags = flags;
            _streamId = streamId;
            _payload = payload;
        }

        public void Parse()
        {
            int bitValue = _rawFlags & 1;
            if (bitValue != 0)
            {
                if (_type == FrameType.Header || _type == FrameType.Data)
                    _flags.Add(FrameFlags.EndStream);

                else if (_type == FrameType.Settings || _type == FrameType.Ping)
                    _flags.Add(FrameFlags.Ack);
            }

            bitValue = _rawFlags & (1 << 2);
            if (bitValue != 0)
            {
                _flags.Add(FrameFlags.EndHeaders);
            }

            bitValue = _rawFlags & (1 << 3);
            if (bitValue != 0)
            {
                _flags.Add(FrameFlags.Padded);
            }

            bitValue = _rawFlags & (1 << 5);
            if (bitValue != 0)
            {
                _flags.Add(FrameFlags.Priority);
            }

            ParsePayload();
        }

        protected virtual Task ParsePayload()
        {
            return Task.CompletedTask;
        }

        public IReadOnlyCollection<FrameFlags> Flags => _flags;

        public FrameType Type => _type;

        public int Length => _length;

        public int StreamId => _streamId;

        public byte[] Payload => _payload;
    }

    public enum FrameFlags : byte
    {
        Unused = 0,
        EndStream,
        Ack,
        EndHeaders,
        Padded,
        Priority
    }

    public enum FrameType : byte
    {
        Data = 0,
        Header,
        Priority,
        RstStream,
        Settings,
        PushPromise,
        Ping,
        GoAway,
        WindowUpdate,
        Continuation
    }
}
