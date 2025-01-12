using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace WoLightning.Clients.Webserver
{
    public enum ConnectionStatusWebserver
    {
        NotStarted = 0,
        NotConnected = 1,
        Unavailable = 2,
        EulaNotAccepted = 3,

        WontRespond = 101,
        Outdated = 102,
        UnknownUser = 103,
        InvalidKey = 104,
        FatalError = 105,
        DevMode = 106,

        Connecting = 199,
        Connected = 200,
    }

    public class ClientWebserver : IDisposable // Todo - Rewrite.
    {
        private readonly Plugin Plugin;

        public ConnectionStatusWebserver Status = ConnectionStatusWebserver.NotStarted;

        private ClientWebSocket? WebSocket;
        private readonly Uri Address = new Uri("wss://localhost:7149");


        public string ServerVersion = string.Empty;

        public ClientWebserver(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {

        }
        public async void Connect()
        {
            if (WebSocket != null) return;

            if (Plugin.Authentification.DevKey.Length == 0)
            {
                Plugin.Log("No Devkey detected - Stopping ClientWebserver creation.");
                Status = ConnectionStatusWebserver.DevMode;
                return;
            }

            if (!Plugin.Authentification.acceptedEula)
            {
                Plugin.Log("Eula isn't accepted - Stopping ClientWebserver creation.");
                Status = ConnectionStatusWebserver.EulaNotAccepted;
                return;
            }

            try
            {
                WebSocket = new ClientWebSocket();
                string text = JsonSerializer.Serialize(new Packet(Plugin, "Test"));

                await WebSocket.ConnectAsync(Address, new CancellationToken());
                await WebSocket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
                Receive();
                JsonSerializer.Deserialize<Packet>(text);
                Plugin.Log(WebSocket.State.ToString());
            }
            catch (Exception ex) { FatalError(ex); }
        }

        public async void Disconnect()
        {
            if (WebSocket == null) return;
            try
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                Plugin.Log(WebSocket.State.ToString());
            }
            catch (Exception ex) { FatalError(ex); }
        }

        public void Send(OperationCode Op) { Send(Op, null, null); }
        public void Send(OperationCode Op, string? OpData) { Send(Op, OpData, null); }
        public async void Send(OperationCode Op, string? OpData, Player? Target)
        {
            if (Status == ConnectionStatusWebserver.Unavailable) return;
            if (WebSocket == null || Plugin.ClientState.LocalPlayer == null) return;
            try
            {
                NetPacket packet = new NetPacket(Op, Plugin.LocalPlayer, OpData, Target);
                string message = JsonSerializer.Serialize(new
                {
                    hash = "n982093c09209jg0920g", // Plugin.Authentification.getHash()
                    devKey = Plugin.Authentification.DevKey,
                    packet,
                });
                await WebSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex) { FatalError(ex); }
        }

        private void processResponse(NetPacket originalPacket, string responseMessage)
        {
            try
            {
                NetPacket? re = JsonSerializer.Deserialize<NetPacket>(responseMessage);
                if (re == null) return;

                if (!re.validate())
                {
                    Plugin.Error("We have received a invalid packet.", re);
                    return;
                }

                if (!re.Sender.equals(Plugin.LocalPlayer) && !re.Target.equals(Plugin.LocalPlayer))
                {
                    Plugin.Error("The received packet is neither from nor for us.", re);
                    return;
                }

                if (re.OpData != null && re.OpData.Equals("Fail-Unauthorized"))
                {
                    Plugin.Error("The server does not remember us sending a request.", re);
                    return;
                }

                if (re.Operation != OperationCode.Ping) Plugin.Log(re);

                string? errorMessage = Plugin.Operation.execute(originalPacket, re);
                if (errorMessage != null)
                {
                    Plugin.Error(errorMessage, re);
                    return;
                }

            }
            catch (Exception ex) { FatalError(ex); }
        }

        private async void Receive()
        {
            if (WebSocket == null) return;
            Status = ConnectionStatusWebserver.Connected;

            try
            {
                List<byte> webSocketPayload = new List<byte>(1024 * 4);
                byte[] MessageBuffer = new byte[1024 * 4];
                bool connectionAlive = true;
                await Task.Run(async () =>
                {
                    while (connectionAlive)
                    {
                        webSocketPayload.Clear();

                        WebSocketReceiveResult? webSocketResponse;
                        do
                        {
                            // Wait until Server sends message
                            webSocketResponse = await WebSocket.ReceiveAsync(MessageBuffer, CancellationToken.None);

                            // Save bytes
                            webSocketPayload.AddRange(new ArraySegment<byte>(MessageBuffer, 0, webSocketResponse.Count));
                        }
                        while (webSocketResponse.EndOfMessage == false);

                        if (webSocketResponse.MessageType == WebSocketMessageType.Text)
                        {
                            // 3. Convert textual message from bytes to string
                            string message = Encoding.UTF8.GetString(webSocketPayload.ToArray());
                            Plugin.Log($"Server says {message}");
                        }
                        else if (webSocketResponse.MessageType == WebSocketMessageType.Close)
                        {
                            // 4. Close the connection
                            Plugin.Log($"Server has closed the Connection.");
                            connectionAlive = false;
                        }
                    }
                });
            }
            catch (Exception ex) { FatalError(ex); }
        }

        internal void FatalError(Exception ex)
        {
            Status = ConnectionStatusWebserver.FatalError;
            try
            {
                WebSocket.Abort();
                WebSocket.Dispose();
            }
            catch (Exception ex2) { Plugin.Log(ex2.ToString()); }
            WebSocket = null;
            Plugin.Log(ex.ToString());
        }
    }
}
