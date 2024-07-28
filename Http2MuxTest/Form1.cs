using Http2Core;
using Http2Core.Frames;
using System.Text;

namespace Http2MuxTest
{
    public partial class Form1 : Form
    {
        private Pipe? _pipe;
        private Multiplexer _clientMux;
        private Multiplexer _serverMux;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly int _clientStreamsCount = 4;
        private readonly object _responseBoxLock = new();
        private readonly List<(FrameStream, List<byte>)> _clientStreams = [];

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _pipe = new Pipe();

            _clientMux = new(_pipe.Stream1);
            _serverMux = new(_pipe.Stream2);

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => SetupServer(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            SetupClient();
        }

        private void SetupClient()
        {
            try
            {
                byte[] testBuffer = [0x41, 0x41, 0x41, 0x41];
                for (int i = 0; i < _clientStreamsCount; i++)
                {
                    FrameStream stream = _clientMux.GetStream();
                    _clientStreams.Add((stream, [.. testBuffer.Select(b => (byte)(b + i))]));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task SetupServer(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    FrameStream? stream = await _serverMux.AcceptAsync(cancellationToken);
                    if (stream == null)
                        break;

                    _ = Task.Run(() => ProcessStreamAsync(stream, cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private async Task ProcessStreamAsync(FrameStream stream, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    Frame? frame = await stream.ReadFrameAsync(cancellationToken);
                    if (frame == null)
                        break;

                    if (frame is HeaderFrame headerFrame)
                    {
                        string message = Encoding.ASCII.GetString(headerFrame.HeaderBlockFragment.ToArray());
                        string headerFields = $"Message Received from Client : \r\nStreamId : {stream.Id}\r\n";
                        headerFields += message + "\r\n";

                        lock (_responseBoxLock)
                        {
                            receiverBox.BeginInvoke(() => AppendMessage(false, headerFields));
                        }

                        string replyMessage = $"I received the message \"{message}\", Thanks\r\n";
                        await stream.WriteFrameAsync(FrameType.Header, 0x5, Encoding.ASCII.GetBytes(replyMessage), cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void AppendMessage(bool isMsgFromServer, string headerFields)
        {
            TextBox control;
            if (isMsgFromServer)
            {
                control = senderBox;
            }
            else
            {
                control = receiverBox;
            }

            control.AppendText(headerFields + "\r\n\r\n");
            control.ScrollToCaret();
        }

        private async void SendRequestBtn_Click(object sender, EventArgs e)
        {
            List<Task> tasks = [];
            foreach ((FrameStream stream, List<byte> headerField) in _clientStreams)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await stream.WriteFrameAsync(FrameType.Header, 0x5, [.. headerField]);
                    Frame? frame = await stream.ReadFrameAsync();
                    if (frame == null)
                        return;

                    if (frame is HeaderFrame headerFrame)
                    {
                        string headerFields = $"Message Received from Server : \r\nStreamId : {stream.Id}\r\n";
                        headerFields += Encoding.ASCII.GetString(headerFrame.HeaderBlockFragment.ToArray()) + "\r\n";

                        lock (_responseBoxLock)
                        {
                            senderBox.BeginInvoke(() => AppendMessage(true, headerFields));
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }
    }
}
