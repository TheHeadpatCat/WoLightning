using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Game.Rules;

namespace WoLightning.WoL_Plugin.Clients
{
    public class WebSocketClient : IDisposable
    {
        private Plugin Plugin { get; set; }
        private Uri Uri { get; set; }
        private ClientWebSocket? Client { get; set; }
        private byte[] receiveBuffer { get; set; } = new byte[1024];

        public bool UpholdConnection = true;
        public int FailedAttempts = 0;

        public Action<string> Received;

        public WebSocketClient(Plugin Plugin, String URL)
        {
            this.Plugin = Plugin;
            Uri = new Uri(URL);
            Client = new ClientWebSocket();
        }

        public WebSocketClient(Plugin Plugin, String URL, String HeaderKey, String HeaderValue)
        {
            try
            {
                this.Plugin = Plugin;
                Uri = new Uri(URL);
                Client = new ClientWebSocket();
                Client.Options.SetRequestHeader(HeaderKey, HeaderValue);
                Client.Options.SetRequestHeader("User-Agent", "WoLightning Plugin");
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Could not create WebSocketClient");
                Client = null;
            }
        }

        public void Dispose()
        {
            UpholdConnection = false;
            FailedAttempts = 99;
            Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None).GetAwaiter().GetResult();
            Client.Dispose();
            Client = null;
        }


        public async Task Connect()
        {
            if (Client == null || !UpholdConnection) return;
            if(FailedAttempts >= 5) { Plugin.Error("Failed 5 Attempts. Aborting Websocket Connection."); UpholdConnection = false; return; }
            if (Client.State == WebSocketState.Open) return;
            try
            {
                Plugin.Log($"[WebSocket] Connecting to {Uri.ToString().Substring(0,8)}...");
                await Client.ConnectAsync(Uri, CancellationToken.None);
                Plugin.Log($"[WebSocket] Successfully Connected to {Uri.ToString().Substring(0,8)}!");
                FailedAttempts = 0;
                Receive();
            }
            catch (Exception ex) {
                Plugin.Error(ex.Message);
                Plugin.Error("Websocket failed to Connect.\nRetrying in 10 seconds.");
                FailedAttempts++;
                Task.Delay(10000).Wait();
                Connect();
            }
        }

        public async Task Send(string message)
        {
            try
            {
                if (Client == null || Client.State != WebSocketState.Open) return;
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                ArraySegment<byte> byteArraySegment = new ArraySegment<byte>(bytes);
                await Client.SendAsync(byteArraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex) { Plugin.Error(ex.Message); }
        }

        private async void Receive()
        {
            try
            {
                if (Client == null || Client.State != WebSocketState.Open) return;
                WebSocketReceiveResult result = await Client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                if(!receivedMessage.Contains("Ping"))Plugin.Log(receivedMessage);
                Received?.Invoke(receivedMessage);
                if (Client.State == WebSocketState.Open) Receive();
                else if (UpholdConnection && (Client.State == WebSocketState.CloseReceived || Client.State == WebSocketState.Closed)) return; Connect();
            }
            catch (Exception ex) { Plugin.Error(ex.Message); }
        }

    }
}
