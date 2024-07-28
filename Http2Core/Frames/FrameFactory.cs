namespace Http2Core.Frames
{
    public static class FrameFactory
    {
        public static Frame Create(int length, FrameType type, byte flags, int streamId, byte[] payload)
        {
            switch (type)
            {
                case FrameType.Header:
                    return new HeaderFrame(length, type, flags, streamId, payload);

                default:
                    throw new NotSupportedException("Invalid FrameType");
            }
        }
    }
}
