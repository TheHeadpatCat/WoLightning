using Lumina.Data.Parsing;
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

        private string[][] Headers { get; set; } = [];
        private byte[] receiveBuffer { get; set; } = new byte[1024];

        public bool UpholdConnection = true;
        public int FailedAttempts = 0;

        public Action<string> Received;

        public WebSocketClient(Plugin Plugin, String URL)
        {
            try
            {
                this.Plugin = Plugin;
                Uri = new Uri(URL);
                Setup();
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Could not create WebSocketClient");
                Client = null;
            }
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
                Plugin.Error(ex.Message);
                Plugin.Error("Could not create WebSocketClient");
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

        public async Task Setup()
        {
            try
            {
                if (Client != null)
                {
                    UpholdConnection = false;
                    FailedAttempts = 99;
                    if(Client.State == WebSocketState.Open) await Client?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
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
            catch(Exception ex)
            {
                Plugin.Log(1,ex.Message);
                Plugin.Log(1,"Failed  to Setup WebSocket");
            }
        }

        private async Task Connect()
        {
            if (Client == null || !UpholdConnection) return;
            if (FailedAttempts >= 5) { Plugin.Error("Failed 5 Attempts. Aborting Websocket Connection."); UpholdConnection = false; return; }
            if (Client.State == WebSocketState.Open) return;
            try
            {
                Plugin.Log(2,$"[WebSocket] Connecting to {Uri.ToString().Substring(0,8)}...");
                await Client.ConnectAsync(Uri, CancellationToken.None);
                Plugin.Log(2,$"[WebSocket] Successfully Connected to {Uri.ToString().Substring(0,8)}!");
                FailedAttempts = 0;
                Receive();
            }
            catch (Exception ex) {
                Plugin.Log(1, ex.Message);
                Plugin.Log(1, "Websocket failed to Connect.\nRetrying in 10 seconds.");
                FailedAttempts++;
                Task.Delay(10000).Wait();
                await Connect();
            }
        }

        public async Task Send(string message)
        {
            try
            {
                if (Client == null || Client.State != WebSocketState.Open)
                {
                    Plugin.Log(2,"WebSocket Request was sent, but Client was Disposed - Resetting Connection.");
                    await Setup();
                    await Send(message);
                    return;
                }
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
                if(!receivedMessage.Contains("Ping"))Plugin.Log(3,"Received Message: " + receivedMessage);
                Received?.Invoke(receivedMessage);
                if (Client.State == WebSocketState.Open) Receive();
                else if (UpholdConnection && (Client.State == WebSocketState.CloseReceived || Client.State == WebSocketState.Closed)) return; await Connect();
            }
            catch (Exception ex) { Plugin.Error(ex.Message); }
        }

    }
}
