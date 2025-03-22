using SharpBiliLiveMessages;
using System.Buffers;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;

namespace Example;

internal class Program
{
    private static readonly SearchValues<char> ctrlChrs = SearchValues.Create("\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0A\x0B\x0C\x0D\x0E\x0F");

    static async Task Main(string[] args)
    {
        Console.WriteLine("Cookie:");
        string cookie = Console.ReadLine()?.Trim() ?? "";
        using SocketsHttpHandler handler_bili = new();handler_bili.UseProxy = true;
        handler_bili.CookieContainer.PerDomainCapacity = 100;
        foreach (string item in cookie.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] kvp = item.Split('=', 2);
            string k = kvp[0].Trim();
            string v = kvp.Length > 1 ? kvp[1].Trim() : "";
            if (k.Any(x => x > '\xFF'))
                k = WebUtility.UrlEncode(k);
            if (v.Any(x => x > '\xFF'))
                v = WebUtility.UrlEncode(v);
            handler_bili.CookieContainer.Add(new Cookie(k, v, "/", ".bilibili.com"));
        }
        using HttpClient http_bili = new(handler_bili)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        http_bili.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        http_bili.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com/");
        XMemberWebAccount? account = await http_bili.GetFromJsonAsync("https://api.bilibili.com/x/member/web/account", SourceGenerationContext.Default.XMemberWebAccount);
        Console.WriteLine("RoomID:");
        long roomID = long.Parse(Console.ReadLine() ?? "1");

        BiliLiveMessageClient? client = await BiliLiveMessageClient.CreateAsync(
            http_bili,
            roomID,
            account?.Data?.MID,
            onReceive: s =>
            {
                Console.WriteLine();
                Console.WriteLine(DateTime.Now.ToString("O"));
                string str = Encoding.UTF8.GetString(s.Span);
                int i = str.AsSpan().IndexOfAny(ctrlChrs);
                if (i != -1)
                {
                    Console.WriteLine($"{i}:{Convert.ToHexString(Encoding.UTF8.GetBytes(str[i..]))}");
                }
                Console.WriteLine(str);
            });
        client?.Wait();
    }
}


public class XMemberWebAccount
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("ttl")]
    public int TTL { get; set; }
    [JsonPropertyName("data")]
    public Data? Data { get; set; }
}

public class Data
{
    [JsonPropertyName("mid")]
    public long MID { get; set; }
    [JsonPropertyName("uname")]
    public string? UName { get; set; }
    [JsonPropertyName("userid")]
    public string? UserID { get; set; }
    [JsonPropertyName("sign")]
    public string? Sign { get; set; }
    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }
    [JsonPropertyName("sex")]
    public string? Sex { get; set; }
    [JsonPropertyName("nick_free")]
    public bool NickFree { get; set; }
    [JsonPropertyName("rank")]
    public string? Rank { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(XMemberWebAccount))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}