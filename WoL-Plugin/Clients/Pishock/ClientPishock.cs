using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util;


namespace WoLightning.Clients.Pishock
{
    public class ClientPishock : IDisposable
    {
        public enum ConnectionStatusPishock
        {
            NotStarted = 0,
            Unavailable = 1,
            InvalidUserdata = 2,
            FatalError = 3,
            ExceededAttempts = 4,

            Connecting = 99,
            ConnectedNoInfo = 100,
            Connected = 200,
        }

        private readonly Plugin? Plugin;
        public ConnectionStatusPishock Status { get; set; } = ConnectionStatusPishock.NotStarted;
        public string UserID { get; set; } = "";
        private WebSocketClient? Client;
        readonly HttpClient HttpClient;

        private string username;
        private string apikey;

        public ClientPishock(Plugin plugin)
        {
            Plugin = plugin;
            HttpClient = new HttpClient();
        }
        public void Dispose()
        {
            if (Client == null) return;
            Client.Received -= OnReceived;
            Client.FailedToConnect -= OnFailedToConnect;
            Client.Connected -= OnConnected;

            Client?.Dispose();
            Client = null;
        }

        public async void Setup()
        {
            if (Client != null)
            {
                Logger.Log(2, "Tried to re-setup Pishock Client, but we are already setup...?");
                return;
            }

            if (Plugin == null || Plugin.Authentification == null)
            {
                Logger.Log(2, "Tried to create Pishock Client, while Plugin or Authentification isnt loaded. Aborting.");
                Status = ConnectionStatusPishock.InvalidUserdata;
                return;
            }

            if (Plugin.Authentification.PishockName.Length < 3 || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Logger.Log(2, "Tried to create Pishock Client, but Data doesnt make sense. Aborting.");
                Status = ConnectionStatusPishock.InvalidUserdata;
                return;
            }

            username = Plugin.Authentification.PishockName;
            apikey = Plugin.Authentification.PishockApiKey;

            await SetupAllData();

            Client = new($"wss://broker.pishock.com/v2?Username={username}&ApiKey={apikey}");
            Client.Received += OnReceived;
            Client.FailedToConnect += OnFailedToConnect;
            Client.Connected += OnConnected;

            await ConnectWebsocket();
        }

        public async Task ConnectWebsocket()
        {
            try
            {
                if (Status == ConnectionStatusPishock.Connecting || Status == ConnectionStatusPishock.InvalidUserdata) return;

                Status = ConnectionStatusPishock.Connecting;

                if (Client == null)
                {
                    Setup();
                    return;
                }

                await Client.Setup();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to create Pishock Socket.");
                Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace);
            }
        }

        private async void OnConnected()
        {
            if (Client == null) return;

            await Client.Send(CommandPublish.Ping());
        }

        private void OnReceived(string obj)
        {
            if (obj.Contains("CONNECTION_ERROR"))
            {
                Status = ConnectionStatusPishock.FatalError;
                Logger.Log(3, "Fatal Error while connecting to Pishock API");
                Logger.Log(3, obj);
                //ConnectWebsocket();
            }

            if (obj.Contains("PONG"))
            {
                if (Client.getState() == System.Net.WebSockets.WebSocketState.Open)
                {
                    Status = ConnectionStatusPishock.Connected;
                    Logger.Log(2, "Successfully connected to Pishock Websocket!");
                }
            }
        }

        private void OnFailedToConnect()
        {
            Status = ConnectionStatusPishock.ExceededAttempts;
            Plugin.NotificationHandler.send("Failed to connect to the Pishock API after several attempts.\nPlease restart the Plugin.", "FATAL ERROR", Dalamud.Interface.ImGuiNotification.NotificationType.Error, new TimeSpan(0, 0, 30));
            Service.ChatGui.PrintError("[WoLightning] Failed to connect to the Pishock API after several attempts.\nIf you are using VPN or similiar, please disable it and restart the Plugin.");
            Logger.Error("Failed to Connect to the Pishock API after 7 attempts. Stopping creation.");
        }

        private async Task SetupAllData()
        {
            if (Plugin == null || Plugin.Authentification == null) return;
            //if (Status != ConnectionStatusPishock.Connecting) return; // We are already setup. Dont run everything again.
            await RequestAccountInformation();
            if (Status != ConnectionStatusPishock.ConnectedNoInfo) return;

            Plugin.Authentification.PishockShockers.Clear();
            await RequestPersonalDevices();
            await RequestSharedDevices();

            if (Status == ConnectionStatusPishock.ConnectedNoInfo)
            {
                Status = ConnectionStatusPishock.Connected;
                Plugin.Configuration.ActivePreset.ValidateShockers();
            }
            else Status = ConnectionStatusPishock.FatalError;
        }

        public async Task RequestAccountInformation()
        {
            HttpResponseMessage Result;
            try
            {

                if (Plugin == null || Plugin.Authentification == null) return;
                string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

                Logger.Log(3, "Requesting Pishock Account information...");

                Result = await HttpClient.GetAsync($"https://auth.pishock.com/Auth/GetUserIfAPIKeyValid?apikey={apikey}&username={username}");
                if (Result.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Error("Could not retrieve UserID from Pishock.");
                    Status = ConnectionStatusPishock.Unavailable;
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to establish a Connection to Pishockm while requesting Account Information.");
                Logger.Error(e);
                Status = ConnectionStatusPishock.FatalError;
                return;
            }
            try
            {

                using (var reader = new StreamReader(Result.Content.ReadAsStream()))
                {
                    string message = reader.ReadToEnd();
                    //Logger.Log(4, message);
                    var parts = message.Split(',');
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("{\"UserId\":"))
                        {
                            UserID = part.Split("{\"UserId\":")[1];
                            break;
                        }
                    }
                }
                if(UserID == null || UserID.Length == 0)
                {
                    Status = ConnectionStatusPishock.InvalidUserdata;
                    Logger.Log(2, "[PI] Userdata was incorrect. Couldn't fetch UserId");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error("Something went wrong while fetching the Pishock UserId.");
                Status = ConnectionStatusPishock.FatalError;
                return;
            }
            Logger.Log(3, "UserID: " + UserID);
            if (UserID.Length > 0) Status = ConnectionStatusPishock.ConnectedNoInfo;
            else Status = ConnectionStatusPishock.InvalidUserdata;
        }

        public async Task RequestPersonalDevices()
        {
            HttpResponseMessage Result;
            try
            {
                string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

                Logger.Log(3, "Requesting Pishock Shocker Information...");

                Result = await HttpClient.GetAsync($"https://ps.pishock.com/PiShock/GetUserDevices?UserId={UserID}&Token={apikey}&api=true");
                if (Result.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Error("Could not retrieve Shocker Information from Pishock.");
                    Status = ConnectionStatusPishock.Unavailable;
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to Connect to Pishock, while requesting Shocker Information.");
                Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace);
                Status = ConnectionStatusPishock.FatalError;
                return;
            }
            Logger.Log(3, " -> Received Shocker Information!");

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    Response[] test = JsonConvert.DeserializeObject<Response[]>(message)!;
                    foreach (var response in test)
                    {
                        foreach (var shocker in response.shockers)
                        {
                            ShockerPishock t = new(shocker.name, response.clientId, shocker.shockerId);
                            Logger.Log(4, t);
                            Plugin.Authentification.PishockShockers.Add(t);

                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }

        public async Task RequestSharedDevices()
        {
            HttpResponseMessage Result;
            try
            {
                string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

                Logger.Log(4, "Requesting Pishock ShareID Information...");

                Result = await HttpClient.GetAsync($"https://ps.pishock.com/PiShock/GetShareCodesByOwner?UserId={UserID}&Token={apikey}&api=true");
                if (Result.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Error("Could not retrieve Sharecode Information from Pishock.");
                    Status = ConnectionStatusPishock.Unavailable;
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to connect to Pishock API, while getting ShareIDs");
                Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace);
                Status = ConnectionStatusPishock.FatalError;
                return;
            }
            Logger.Log(3, " -> Received ShareID Information!");

            List<string> ShareIds = [];

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    Logger.Log(4, message);
                    if (message == null || message.Length <= 5) return;
                    string[] parts = message.Split("],");
                    if (parts.Length < 2) return;
                    foreach (string part in parts)
                    {
                        string Tpart = part;
                        Tpart = Tpart.Replace("{", "");
                        Tpart = Tpart.Replace("}", "");
                        Tpart = Tpart.Replace("[", "");
                        Tpart = Tpart.Replace("]", "");

                        string name = Tpart.Split("\"")[1];
                        string[] shareIds = Tpart.Split(":")[1].Split(",");
                        foreach (string shareid in shareIds)
                        {
                            ShareIds.Add(shareid);
                            Logger.Log(4, "Shareid: " + shareid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    return;
                }
            }
            // Got All Sharecodes - request Information now
            if (ShareIds.Count == 0) return;

            Logger.Log(3, "Sharecodes received. Requesting Shocker Information...");

            string URL = $"https://ps.pishock.com/PiShock/GetShockersByShareIds?UserId={UserID}&Token={apikey}&api=true";
            foreach (string shareId in ShareIds)
            {
                URL += $"&shareIds={shareId}";
            }

            Result = await HttpClient.GetAsync(URL);
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Could not retrieve Shared Shocker Information from Pishock.");
                return;
            }

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    //Logger.Log(message);

                    string[] parts = message.Split("],");
                    foreach (string part in parts)
                    {
                        string Tpart = part;
                        string name = Tpart.Split("\"")[1];
                        string information = Tpart.Substring(name.Length + 3);
                        if (information.StartsWith(":")) information = information.Substring(1);
                        if (information.EndsWith("}]}")) information = information.Replace("}]}", "}");
                        information += "]";

                        SharedResponse[] test = JsonConvert.DeserializeObject<SharedResponse[]>(information)!;
                        foreach (SharedResponse response in test)
                        {
                            Logger.Log(4, response);
                            if (name.ToLower().Equals(Plugin.Authentification.PishockName.ToLower())) continue;
                            ShockerPishock shocker = new(response.shockerName, response.clientId, response.shockerId);
                            shocker.isPersonal = false;
                            shocker.username = name;
                            shocker.shareId = response.shareId;
                            shocker.shareCode = response.shareCode;
                            Plugin.Authentification.PishockShockers.Add(shocker);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    return;
                }
            }
        }

        public async void SendRequest(ShockOptions Options)
        {

            #region Validation
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Logger.Log(3, " -> Aborted due to invalid Account Settings!");
                return;
            }

            if (Plugin.IsFailsafeActive)
            {
                Logger.Log(3, " -> Blocked request due to failsafe mode!");
                return;
            }

            if (!Options.Validate())
            {
                Logger.Log(3, " -> Blocked due to invalid ShockOptions!");
                return;
            }

            if (Options.ShockersPishock.Count == 0)
            {
                Logger.Log(3, " -> No Pishock Shockers assigned, discarding!");
                return;
            }
            #endregion


            if (Client == null || Status != ConnectionStatusPishock.Connected)
            {
                if (Status == ConnectionStatusPishock.Connecting) return;
                await ConnectWebsocket();
                return;
            }

            if (Options.WarningMode != WarningMode.None)
            {
                ShockOptions warningOptions = new ShockOptions(Options);
                warningOptions.OpMode = OpMode.Vibrate;
                warningOptions.Intensity = 55;
                warningOptions.Duration = 1;
                string sendWarning = CommandPublish.Generate(warningOptions, UserID, true);
                await Client.Send(sendWarning);
                int delay;
                switch (Options.WarningMode)
                {
                    case WarningMode.Short: delay = new Random().Next(3000, 5000); break;
                    case WarningMode.Medium: delay = new Random().Next(7000, 12000); break;
                    case WarningMode.Long: delay = new Random().Next(12000, 27000); break;
                    default: delay = 2000; break;
                }
                Logger.Log(3, "[PI] -> Warning sent!\nWaiting " + delay + "ms...");
                await Task.Delay(delay);
            }

            string sendCommand = CommandPublish.Generate(Options, UserID, null);
            if (sendCommand == "Invalid")
            {
                Logger.Log(3, "[PI] -> Failed to generate CommandPublish.");
                return;
            }
            await Client.Send(sendCommand);
            Logger.Log(3, "[PI] -> Command sent!");
        }




    }
}
