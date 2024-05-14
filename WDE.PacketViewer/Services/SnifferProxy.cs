using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Services;

[AutoRegister]
public class SnifferProxy : ISnifferProxy
{
    private int port = 12346;

    public event Action<ParserRawPacket>? OnSnifferPacketArrived;

    public SnifferProxy()
    {
    }

    public async Task Listen(CancellationToken token)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        try
        {
            while (true)
            {
                var connection = await listener.AcceptTcpClientAsync(token);

                token.ThrowIfCancellationRequested();

                Worker(connection, token).ListenErrors();
            }
        }
        finally
        {
            listener.Stop();
            listener.Dispose();
        }
    }

    private async Task Worker(TcpClient connection, CancellationToken token)
    {
        try
        {
            var stream = connection.GetStream();
            byte[] header = new byte[4 + 4 + 8 + 4 + 4];
            byte[] buffer = Array.Empty<byte>();
            while (true)
            {
                await stream.ReadExactlyAsync(header, cancellationToken: token);
                token.ThrowIfCancellationRequested();

                var isSmsg = header[0] == 'S';
                var sessionId = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 4));
                var tickCount = (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(header, 8));
                var additionalDataLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 16));
                var bufferSize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 20));
                if (buffer.Length < additionalDataLength || buffer.Length < bufferSize)
                    Array.Resize(ref buffer, Math.Max(additionalDataLength, bufferSize));
                await stream.ReadExactlyAsync(buffer, 0, additionalDataLength, token);
                token.ThrowIfCancellationRequested();

                await stream.ReadExactlyAsync(buffer, 0, bufferSize, token);
                token.ThrowIfCancellationRequested();

                var opcode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));

                var bufferCopy = new byte[bufferSize - 4];
                Array.Copy(buffer, 4, bufferCopy, 0, bufferSize - 4);

                var packet = new ParserRawPacket()
                {
                    IsSmsg = isSmsg,
                    SessionId = sessionId,
                    TickCount = tickCount,
                    Opcode = opcode,
                    Data = bufferCopy
                };
                OnSnifferPacketArrived?.Invoke(packet);
            }
        }
        catch (EndOfStreamException)
        {
            // client disconnected
            connection.Close();
        }
        finally
        {
            connection.Close();
        }
    }
}