using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Clients.Pishock;
using WoLightning.Configurations;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util;
using static WoLightning.Clients.Pishock.ClientPishock;

namespace WoLightning.WoL_Plugin.Windows
{
    public class DebugWindow : Window, IDisposable
    {
        private readonly Plugin Plugin;

        private WebSocketClient testClient;
        private TimerPlus clientTimer;
        private int failedAttempts = 0;
        private DateTime? successful;
        public DebugWindow(Plugin plugin)
            : base("WoLightning - DebugWindow")
        {
            Plugin = plugin;

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(25, 25),
                MaximumSize = new Vector2(2000, 2000)
            };
            clientTimer = new();
        }

        public void Dispose() {

            if (testClient != null)
            {
                testClient.Dispose();
            }

            if (clientTimer != null)
            {
                clientTimer.Dispose();
                clientTimer.Elapsed -= RunCode;
            }
        }
        public override async void Draw()
        {
            ImGui.TextColored(new Vector4(255, 0, 0, 255), "You should not be touching these settings, if you don't know what you are doing.");


            if (Plugin == null) return;


            ImGui.Text("timer : " + Plugin.Configuration.ActivePreset.ForgetPartnerBuff.RepeatTimer.Enabled);



        }

        public async void RunCode(object? sender, ElapsedEventArgs e)
        {
            Logger.Log(4, "[PishockTest] Creating new Websocket...");
            if(testClient != null)
            {
                testClient.Dispose();
                testClient = null;
                Logger.Log(4, "[PishockTest] Disposing old Websocket...");
            }

            string username = Plugin.Authentification.PishockName;
            string apikey = Plugin.Authentification.PishockApiKey;

            testClient = new($"wss://broker.pishock.com/v2?Username={username}&ApiKey={apikey}");

            testClient.Received += OnReceived;
            testClient.FailedToConnect += OnFailedToConnect;
            testClient.Connected += OnConnected;

            ConnectWebsocket();
        }

        private async void OnConnected()
        {
            await testClient.Send(CommandPublish.Ping());
        }

        private void OnReceived(string obj)
        {
            if (obj.Contains("CONNECTION_ERROR"))
            {
                Logger.Log(4, "[PishockTest] Failed to Connect");
                Plugin.NotificationHandler.send("Pishock Test failed", "Pishock Test failed", Dalamud.Interface.ImGuiNotification.NotificationType.Error, new TimeSpan(0, 0, 30));
                failedAttempts++;
            }

            if (obj.Contains("PONG"))
            {
                if (testClient.getState() == System.Net.WebSockets.WebSocketState.Open)
                {
                    Logger.Log(4, "[PishockTest] Successfully connected to Pishock Websocket!");
                    Plugin.NotificationHandler.send("PISHOCK TEST SUCCESS", "PISHOCK TEST SUCCESS",Dalamud.Interface.ImGuiNotification.NotificationType.Success, new TimeSpan(0,2,0));
                    clientTimer.Stop();
                    successful = DateTime.Now;
                }
            }
        }

        private void OnFailedToConnect()
        {
            
        }

        public async Task ConnectWebsocket()
        {
            try
            {
                await testClient.Setup();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create Pishock Socket.");
                Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace);
            }
        }
    }
}
