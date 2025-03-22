using SharpBiliLiveMessages.HTTPAPI.Xlive.WebRoom.V1.Index.GetDanmuInfo;
using SharpBiliLiveMessages.Network;
using SharpBiliLiveMessages.Network.Requests;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;
using Timer = System.Timers.Timer;

namespace SharpBiliLiveMessages;

public sealed class BiliLiveMessageClient : IDisposable
{
    private readonly WebSocket? wsClient;
    private readonly TcpClient? tcpClient;
    private readonly Stream stream;
    private readonly PacketSender sender;
    private readonly PacketReceiver receiver;
    private readonly Timer timer;
    private readonly Dictionary<ulong, ManualResetEventSlim> popularityCheckEvents = [];
    private readonly bool sendTogether;
    private ulong counter = 0;
    private bool disposed;

    public uint Popularity { get; private set; }
    public ConnectMethod Method { get; }
    public Action<Memory<byte>>? OnReceive { get; set; }

    public BiliLiveMessageClient(Verification verification, HostInfo host, ConnectMethod method, Action<Memory<byte>>? onReceive = null, int heartbeatInterval = 30000, Func<Uri, WebSocket>? wsCreator = null)
    {
        Method = method;
        OnReceive = onReceive;
        switch (method)
        {
            case ConnectMethod.TCP:
                tcpClient = new(host.Host!, host.Port);
                stream = tcpClient.GetStream();
                break;
            case ConnectMethod.WS:
                ArgumentNullException.ThrowIfNull(wsCreator);
                Uri ws = new($"ws://{host.Host}:{host.WSPort}/sub");
                wsClient = wsCreator(ws);
                stream = new WebSocketStream(wsClient);
                break;
            case ConnectMethod.WSS:
                ArgumentNullException.ThrowIfNull(wsCreator);
                Uri wss = new($"wss://{host.Host}:{host.WSSPort}/sub");
                wsClient = wsCreator(wss);
                stream = new WebSocketStream(wsClient);
                break;
            default:
                throw new ArgumentException($"Incorrect method: {method}", nameof(method));
        }
        sendTogether = Method != ConnectMethod.TCP;
        sender = new(stream);
        byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(verification, SourceGenerationContext.Default.Verification);
        PrivateSend(PacketType.Verification, buffer);
        receiver = new(stream, PrivateReceive);
        timer = new(new TimeSpan(0, 0, 0, 0, heartbeatInterval))
        {
            AutoReset = true,
        };
        timer.Elapsed += PrivateHeartbeat;
        timer.Start();
    }

    public static async Task<BiliLiveMessageClient?> CreateAsync(HttpClient http, long roomID, long? uid, ConnectMethod method = ConnectMethod.TCP, Action<Memory<byte>>? onReceive = null, Func<Uri, WebSocket>? wsCreator = null)
    {
        using HttpRequestMessage req = new(HttpMethod.Get, DanmuInfo.API + roomID);
        req.Headers.Referrer = new("https://live.bilibili.com");
        using HttpResponseMessage resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        DanmuInfo info = await resp.Content.ReadFromJsonAsync(SourceGenerationContext.Default.DanmuInfo) ?? throw new Exception();
        if (info?.Data?.Token is null || info.Data.HostList is null)
            throw new Exception($"{info?.Code} {info?.Message}");
        Verification v = new()
        {
            Key = info.Data.Token,
            RoomID = roomID,
            UID = uid
        };
        BiliLiveMessageClient? client = null;
        foreach (HostInfo host in info.Data.HostList)
        {
            try
            {
                BiliLiveMessageClient c = new(v, host, method, onReceive, wsCreator: wsCreator);
                client = c;
                break;
            }
            catch
            {
                continue;
            }
        }
        return client;
    }

    private void PrivateHeartbeat(object? s, ElapsedEventArgs e)
    {
        PrivateSend(PacketType.Heartbeat, []);
    }
    private void PrivateSend(PacketType type, Span<byte> body)
    {
        switch (type)
        {
            case PacketType.Heartbeat:
            case PacketType.HeartbeatResponse:
            case PacketType.Verification:
            case PacketType.VerificationResponse:
                sender.Send(PacketProtocol.HeartbeatOrVerification, type, body, sendTogether);
                break;
            default:
                sender.Send(PacketProtocol.UncompressedNormal, type, body, sendTogether);
                break;
        }
    }
    private void PrivateReceive(PacketHeader header, ArrayPoolWrapper<byte> body)
    {
        Span<byte> bytes = body;
        switch (header.Type)
        {
            case PacketType.HeartbeatResponse when bytes.Length >= 4:
                Popularity = (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
                ReadOnlySpan<byte> payload = bytes[4..];
                if (payload.Length == 8)
                {
                    ulong id = Unsafe.ReadUnaligned<ulong>(ref MemoryMarshal.GetReference(payload));
                    if (popularityCheckEvents.TryGetValue(id, out ManualResetEventSlim? ev))
                        ev.Set();
                }
                break;
            case PacketType.VerificationResponse:
                break;
            case PacketType.Normal:
                switch (header.Protocol)
                {
                    case PacketProtocol.ZlibNormal:
                        using (MemoryStream ms = new())
                        using (ZLibStream zs = new(ms, CompressionMode.Decompress))
                        {
                            zs.Write(body);
                            OnReceive?.Invoke(ms.ToArray());
                        }
                        break;
                    case PacketProtocol.BrotliNormal:
                        using (MemoryStream ms = new(body.UnderlyingBuffer, 0, body.Length))
                        using (BrotliStream bs = new(ms, CompressionMode.Decompress))
                        {
                            PacketHeader innerHeader = new();
                            while (innerHeader.ReadFromStream(bs))
                            {
                                using ArrayPoolWrapper<byte> apw = new((int)innerHeader.ContentLength);
                                bs.ReadExactly(apw);
                                OnReceive?.Invoke(apw);
                            }
                        }
                        break;
                    default:
                        OnReceive?.Invoke(body);
                        break;
                }
                break;
        }
    }

    public uint? UpdatePopularity(int timeout = 5000)
    {
        using ManualResetEventSlim ev = new(false);
        ulong value = counter++;
        Span<byte> buffer = stackalloc byte[8];
        uint? result = null;
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer), value);
        if (popularityCheckEvents.TryAdd(value, ev))
        {
            PrivateSend(PacketType.Heartbeat, buffer);
            if (ev.Wait(timeout))
                result = Popularity;
            popularityCheckEvents.Remove(value);
            ev.Dispose();
        }
        return result;
    }

    public void Wait()
    {
        receiver.Wait();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            timer?.Dispose();
            stream?.Dispose();
            wsClient?.Dispose();
            tcpClient?.Dispose();
            foreach ((_, ManualResetEventSlim ev) in popularityCheckEvents)
            {
                ev?.Dispose();
            }
            receiver.Stop();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

public enum ConnectMethod
{
    TCP,
    WS,
    WSS
}