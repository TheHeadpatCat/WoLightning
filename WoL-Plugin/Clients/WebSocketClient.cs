using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Clients
{
    public class WebSocketClient : IDisposable // todo: rewrite this garbage
    {
        private Plugin Plugin { get; set; }
        private Uri Uri { get; set; }
        private ClientWebSocket? Client { get; set; }

        private string[][] Headers { get; set; } = [];
        private byte[] receiveBuffer { get; set; } = new byte[1024];

        public bool UpholdConnection = true;
        public int FailedAttempts = 0;

        public Action<string> Received;
        private TimerPlus Heartbeat;

        public WebSocketClient(Plugin Plugin, String URL)
        {
            try
            {
                this.Plugin = Plugin;
                Heartbeat = new();
                Heartbeat.Interval = 5000;
                Heartbeat.AutoReset = true;
                Heartbeat.Elapsed += checkHeartbeat;
                Uri = new Uri(URL);
                Setup();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error("Could not create WebSocketClient");
                Client = null;
            }
        }

        private void checkHeartbeat(object? sender, ElapsedEventArgs e)
        {
            if (Plugin == null || Client == null)
            {
                Logger.Log(2, "Heartbeat failed - Killing Client.");
                Dispose();
            }
        }

        public WebSocketState getState()
        {
            return Client.State;
        }

        public WebSocketClient(Plugin Plugin, String URL, string[][] Headers)
        {
            try
            {
                this.Plugin = Plugin;
                Uri = new Uri(URL);
                this.Headers = Headers;
                Setup();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error("Could not create WebSocketClient");
                Client = null;
            }
        }

        public void Dispose()
        {
            try
            {
                UpholdConnection = false;
                FailedAttempts = 99;
                Client?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None).GetAwaiter().GetResult();
                Client?.Dispose();
                Client = null;
            }
            catch { }
        }

        private async Task Setup()
        {
            try
            {
                if (Plugin == null)
                {
                    Dispose();
                    return;
                }
                if (Client != null)
                {
                    UpholdConnection = false;
                    FailedAttempts = 99;
                    if (Client.State == WebSocketState.Open) await Client?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
                    Client.Dispose();
                    Client = null;
                }

                Client = new ClientWebSocket();
                Client.Options.SetRequestHeader("User-Agent", "WoLightning Plugin");

                foreach (string[] keyValuePair in Headers)
                {
                    if (keyValuePair.Length < 2) continue;
                    Client.Options.SetRequestHeader(keyValuePair[0], keyValuePair[1]);
                }

                UpholdConnection = true;
                FailedAttempts = 0;
                await Connect();
            }
            catch (Exception ex)
            {
                Logger.Log(1, ex.Message);
                Logger.Log(1, "Failed  to Setup WebSocket");
            }
        }

        private async Task Connect()
        {
            if (Plugin == null)
            {
                Dispose();
                return;
            }
            if (Client == null || !UpholdConnection) return;
            if (FailedAttempts >= 5) { Logger.Error("Failed 5 Attempts. Aborting Websocket Connection."); UpholdConnection = false; return; }
            if (Client.State == WebSocketState.Open) return;
            try
            {
                Logger.Log(2, $"[WebSocket] Connecting to {Uri.ToString().Substring(0, 16)}...");
                await Client.ConnectAsync(Uri, CancellationToken.None);
                Logger.Log(2, $"[WebSocket] Successfully Connected to {Uri.ToString().Substring(0, 16)}!");
                FailedAttempts = 0;
                Receive();
            }
            catch (Exception ex)
            {
                Logger.Log(3, ex.Message);
                Logger.Log(3, "Websocket failed to Connect.");
                FailedAttempts++;

                if (Client.State == WebSocketState.Open) return;
                if (Client.State == WebSocketState.Connecting) return;
                if (ex.Message.Contains("already been started.")) return;

                Task.Delay(10000).Wait();
                await Connect();
            }
        }

        public async Task Send(string message)
        {
            try
            {
                if (Plugin == null)
                {
                    Dispose();
                    return;
                }
                if (Client == null || Client.State == WebSocketState.Closed)
                {
                    Logger.Log(2, "WebSocket Request was sent, but Client was Disposed - Resetting Connection.");
                    await Setup();
                    await Send(message);
                    return;
                }
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                ArraySegment<byte> byteArraySegment = new ArraySegment<byte>(bytes);
                await Client.SendAsync(byteArraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex) { Logger.Log(3, "Sending Message Failed."); Logger.Log(3, ex.Message); }
        }

        private async void Receive()
        {
            try
            {
                if (Plugin == null)
                {
                    Dispose();
                    return;
                }
                if (Client == null || Client.State != WebSocketState.Open) return;
                WebSocketReceiveResult result = await Client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                Logger.Log(4, "Received Message: " + receivedMessage);
                Received?.Invoke(receivedMessage);
                if (UpholdConnection && Client.State == WebSocketState.Open) Receive();

                /*else if (UpholdConnection && (Client.State == WebSocketState.CloseReceived || Client.State == WebSocketState.Closed))
                {
                    await Client.CloseAsync(WebSocketCloseStatus.InternalServerError, "Error Received", CancellationToken.None);
                }*/
            }
            catch (Exception ex) { Logger.Log(3, "Receiving Message failed."); Logger.Log(3, ex.Message); }
        }

    }
}
