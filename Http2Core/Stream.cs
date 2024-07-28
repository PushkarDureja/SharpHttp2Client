using Http2Core.Frames;
using System.Collections.Concurrent;

namespace Http2Core
{
    public class Stream
    {
        private readonly int _id;
        private readonly StreamMultiplexer _mux;
        private readonly int _readTimeout = -1;
        private readonly ConcurrentQueue<Frame> _pendingReadFrames = new();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        volatile bool _fin;
        volatile int _disposed;
        TaskCompletionSource? _taskCompletionSource;

        public Stream(int id, StreamMultiplexer mux)
        {
            _id = id;
            _mux = mux;
        }

        public async Task DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
                return;

            await WriteToStream(null);
        }

        public async Task<Frame?> ReadAsync(CancellationToken cancellationToken = default)
        {
            bool semaphoreReleased = false;
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_pendingReadFrames.IsEmpty)
                {
                    if (_fin)
                        return null;

                    _taskCompletionSource = new TaskCompletionSource();

                    Task delayTask = Task.Delay(_readTimeout, cancellationToken);
                    Task readTask = _taskCompletionSource.Task;

                    _semaphore.Release();
                    semaphoreReleased = true;

                    if (await Task.WhenAny(delayTask, readTask) == delayTask)
                    {
                        _taskCompletionSource.TrySetCanceled(cancellationToken);
                        throw new IOException();
                    }
                    semaphoreReleased = false;
                    await _semaphore.WaitAsync(cancellationToken);
                }

                if (_fin)
                    return null;

                bool res = _pendingReadFrames.TryDequeue(out Frame? result);
                if (!res || result is null)
                {
                    throw new IOException();
                }

                return result;
            }
            finally
            {
                if (!semaphoreReleased)
                {
                    _semaphore.Release();
                }
            }
        }

        public async Task WriteToStream(Frame? frame, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (frame is null)
                {
                    _fin = true;
                    _taskCompletionSource?.TrySetResult();
                    return;
                }

                _pendingReadFrames.Enqueue(frame);
                _taskCompletionSource?.TrySetResult();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task WriteAsync(FrameType type, byte flags, byte[] payload, CancellationToken cancellationToken = default)
        {
            await _mux.WriteFrame(Id, type, flags, payload, cancellationToken);
        }

        public int Id => _id;
    }
}
