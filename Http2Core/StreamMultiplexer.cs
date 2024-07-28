using Http2Core.Frames;
using System.Collections.Concurrent;

namespace Http2Core
{
    public class StreamMultiplexer
    {
        private readonly int _maxStreamCount;
        private readonly System.IO.Stream _stream;
        private readonly CancellationTokenSource _tokenSource;
        private readonly ConcurrentDictionary<int, Stream> _streams = new();
        private readonly ConcurrentQueue<Stream> _newStreams = new();

        private bool _disposed;
        private volatile int _lastStreamId = 0;
        private TaskCompletionSource _acceptTaskSource = new TaskCompletionSource();

        public StreamMultiplexer(System.IO.Stream stream, int maxConcurrentStreamCount = 100)
        {
            _stream = stream;
            _maxStreamCount = maxConcurrentStreamCount;
            _tokenSource = new CancellationTokenSource();
            _ = Task.Run(() => ReadFrameAsync(_tokenSource.Token), _tokenSource.Token);
        }

        public Stream GetStream()
        {
            int newStreamId;
            int originalStreamId;

            do
            {
                originalStreamId = _lastStreamId;

                if (originalStreamId < 1)
                {
                    newStreamId = 1;
                }
                else if (originalStreamId + 2 < _maxStreamCount)
                {
                    newStreamId = originalStreamId + 2;
                }
                else
                {
                    throw new IOException("Max Concurrent Channel Count Exceeded");
                }
            } while (Interlocked.CompareExchange(ref _lastStreamId, newStreamId, originalStreamId) != originalStreamId);

            Stream stream = new(newStreamId, this);
            if (!_streams.TryAdd(newStreamId, stream))
            {
                throw new IOException("StreamId already exists");
            }

            return stream;
        }

        public async Task<Stream?> AcceptAsync(CancellationToken cancellationToken)
        {
            if (_newStreams.IsEmpty)
                await _acceptTaskSource.Task;

            if (_newStreams.IsEmpty)
                return null;

            if (_newStreams.TryDequeue(out Stream? stream) && stream != null)
            {
                _acceptTaskSource = new TaskCompletionSource();
                return stream;
            }

            return null;
        }

        public async Task DisposeAsync()
        {
            if (_disposed)
                return;

            _tokenSource.Cancel();
            _acceptTaskSource.SetResult();

            foreach (KeyValuePair<int, Stream> item in _streams)
            {
                await item.Value.DisposeAsync();
            }
            _streams.Clear();

            try
            {
                _stream.Dispose();
            }
            catch (ObjectDisposedException) { }
            try
            {
                _tokenSource.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        public async Task WriteFrame(int streamId, FrameType type, byte flags, byte[] payload, CancellationToken cancellationToken = default)
        {
            int payloadLength = payload.Length;
            byte[] frameBuffer = new byte[9 + payloadLength];
            BinaryWriter frameWriter = new BinaryWriter(new MemoryStream(frameBuffer));

            frameWriter.Write(new byte[] { (byte)((payloadLength >> 16) & 0xff), (byte)((payloadLength >> 8) & 0xff), (byte)((payloadLength) & 0xff) });
            frameWriter.Write((byte)type);
            frameWriter.Write(flags);
            frameWriter.Write(new byte[] { (byte)((streamId >> 24) & ((1 << 7) - 1)), (byte)((streamId >> 16) & 0xff), (byte)((streamId >> 8) & 0xff), (byte)((streamId) & 0xff) });
            frameWriter.Write(payload);
            frameWriter.Flush();

            await _stream.WriteAsync(frameBuffer, cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }

        private async Task ProcessFrame(Frame frame, CancellationToken cancellationToken)
        {
            Stream newStream = new(frame.StreamId, this);

            Stream stream = _streams.GetOrAdd(frame.StreamId, newStream);
            if (stream == newStream)
            {
                _newStreams.Enqueue(newStream);
                _acceptTaskSource.SetResult();
            }

            await stream.WriteToStream(frame, cancellationToken);
        }

        private async Task ReadFrameAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] frameHeader = new byte[9];
                    int bytesRead = await _stream.ReadAsync(frameHeader.AsMemory(0, 9), cancellationToken);
                    if (bytesRead < 1)
                        break;

                    int length = frameHeader[2] | (frameHeader[1] << 8) | (frameHeader[0] << 16);

                    byte type = frameHeader[3];
                    if (!Enum.IsDefined(typeof(FrameType), type))
                    {
                        throw new NotSupportedException("Invalid Frame Type");
                    }

                    byte flags = frameHeader[4];

                    int streamId = frameHeader[8] | frameHeader[7] << 8 | frameHeader[6] << 16 | ((frameHeader[5] << 24) & ((1 << 7) - 1));

                    byte[] payload = new byte[length];
                    bytesRead = await _stream.ReadAsync(payload.AsMemory(0, length), cancellationToken);
                    if (bytesRead < 1)
                        break;

                    Frame frame = FrameFactory.Create(length, (FrameType)type, flags, streamId, payload);
                    frame.Parse();

                    await ProcessFrame(frame, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                await DisposeAsync();
            }
        }

        public int MaxStreamCount => _maxStreamCount;
    }
}
