using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FuelTrack.Api.Tests.Notifications;

internal sealed class LocalGateway : IAsyncDisposable
{
    private readonly HttpListener listener = new();
    private readonly Task loop;
    public readonly ConcurrentQueue<(string Body, string? Auth, string? Idempotency)> Requests = new();
    public int Status { get; set; } = 200;
    public string Response { get; set; } = "{\"messageId\":\"local-test-id\"}";
    public int DelayMs { get; set; }
    public string Url { get; }
    public LocalGateway()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0); socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        Url = $"http://127.0.0.1:{port}/"; listener.Prefixes.Add(Url); listener.Start(); loop = LoopAsync();
    }
    private async Task LoopAsync()
    {
        try
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                using var reader = new StreamReader(context.Request.InputStream);
                Requests.Enqueue((await reader.ReadToEndAsync(), context.Request.Headers["Authorization"], context.Request.Headers["Idempotency-Key"]));
                if (DelayMs > 0) await Task.Delay(DelayMs);
                try
                {
                    context.Response.StatusCode = Status; context.Response.ContentType = "application/json";
                    var bytes = Encoding.UTF8.GetBytes(Response); context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close();
                }
                catch (Exception e) when (e is HttpListenerException or ObjectDisposedException or IOException) { }
            }
        }
        catch (Exception e) when (e is HttpListenerException or ObjectDisposedException) { }
    }
    public async ValueTask DisposeAsync() { listener.Close(); await loop; }
}

internal sealed class RejectingSmtpServer : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stop = new();
    private readonly Task run;
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public RejectingSmtpServer(int code) { listener.Start(); run = RunAsync(code); }
    private async Task RunAsync(int code)
    {
        try
        {
            using var connection = await listener.AcceptTcpClientAsync(stop.Token);
            using var reader = new StreamReader(connection.GetStream(), Encoding.ASCII);
            using var writer = new StreamWriter(connection.GetStream(), Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            await writer.WriteLineAsync("220 localhost SMTP test");
            while (!stop.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(stop.Token); if (line == null) break;
                if (line.StartsWith("EHLO") || line.StartsWith("HELO")) await writer.WriteLineAsync("250 localhost");
                else if (line.StartsWith("QUIT")) { await writer.WriteLineAsync("221 Bye"); break; }
                else await writer.WriteLineAsync($"{code} controlled test error with fake password NEVER_PERSIST_ME");
            }
        }
        catch (Exception e) when (e is OperationCanceledException or IOException or ObjectDisposedException) { }
    }
    public async ValueTask DisposeAsync() { stop.Cancel(); listener.Stop(); await run; stop.Dispose(); }
}
