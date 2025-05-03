using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Game.Rules;
using static Dalamud.Interface.Utility.Raii.ImRaii;


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

            Connecting = 99,
            ConnectedNoInfo = 100,
            Connected = 200,
        }

        private Plugin? Plugin;
        public ConnectionStatusPishock Status { get; set; } = ConnectionStatusPishock.NotStarted;
        public string UserID { get; set; } = "";
        private WebSocketClient? Client;

        public ClientPishock(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {
            if (Client != null)
            {
                Client.Dispose();
                Client = null;
            }
        }

        public async void Setup()
        {
            if(Client != null)
            {
                Client.Dispose();
                Client = null;
            }

            await CreateSocket();
            await SetupAllData();
        }

        public void Test()
        {
            
        }

        public async Task CreateSocket()
        {
            if (Client != null || Plugin == null || Plugin.Authentification == null) return;

            if (Plugin.Authentification.PishockName.Length < 3 || Plugin.Authentification.PishockApiKey.Length < 16) return;

            Status = ConnectionStatusPishock.Connecting;

            string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;
            Client = new(Plugin, $"wss://broker.pishock.com/v2?Username={username}&ApiKey={apikey}");

            await Client.Setup();
        }

        public async Task SetupAllData()
        {
            if (Client == null || Plugin == null || Plugin.Authentification == null) return;
            if (Status != ConnectionStatusPishock.Connecting) return; // We are already setup. Dont run everything again.
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
            if (Plugin == null || Plugin.Authentification == null) return;
            string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

            Plugin.Log(3,"Requesting Pishock Account information...");

            HttpClient HttpClient = new();
            var Result = await HttpClient.GetAsync($"https://auth.pishock.com/Auth/GetUserIfAPIKeyValid?apikey={apikey}&username={username}");
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve UserID from Pishock.");
                Status = ConnectionStatusPishock.Unavailable;
                return;
            }
            try
            {

                using (var reader = new StreamReader(Result.Content.ReadAsStream()))
                {
                    string message = reader.ReadToEnd();
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
            }
            catch (Exception ex) {
                Plugin.Error(ex.Message);
                Plugin.Error("Something went wrong while fetching the Pishock UserId.");
                Status = ConnectionStatusPishock.FatalError;
                return;
            }
            Plugin.Log(3,"UserID: " + UserID);
            if (UserID.Length > 0) Status = ConnectionStatusPishock.ConnectedNoInfo;
            else Status = ConnectionStatusPishock.InvalidUserdata;
        }

        public async Task RequestPersonalDevices()
        {
            string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

            Plugin.Log(3,"Requesting Pishock Shocker Information...");

            HttpClient HttpClient = new();
            var Result = await HttpClient.GetAsync($"https://ps.pishock.com/PiShock/GetUserDevices?UserId={UserID}&Token={apikey}&api=true");
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Shocker Information from Pishock.");
                Status = ConnectionStatusPishock.Unavailable;
                return;
            }

            Plugin.Log(3," -> Received Shocker Information!");

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
                            ShockerPishock t = new ShockerPishock(shocker.name, response.clientId, shocker.shockerId);
                            Plugin.Log(3,t);
                            Plugin.Authentification.PishockShockers.Add(t);
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.Message);
                }
            }
        }

        public async Task RequestSharedDevices()
        {
            string username = Plugin.Authentification.PishockName, apikey = Plugin.Authentification.PishockApiKey;

            Plugin.Log(3,"Requesting Pishock ShareID Information...");

            HttpClient HttpClient = new();
            var Result = await HttpClient.GetAsync($"https://ps.pishock.com/PiShock/GetShareCodesByOwner?UserId={UserID}&Token={apikey}&api=true");
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Sharecode Information from Pishock.");
                Status = ConnectionStatusPishock.Unavailable;
                return;
            }
            Plugin.Log(3," -> Received ShareID Information!");

            List<string> ShareIds = new List<string>();

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    Plugin.Log(3,message);
                    if (message == null || message.Length == 0) return;
                    string[] parts = message.Split("],");
                    if (parts.Length < 1) return;
                    foreach(string part in parts)
                    {
                        string Tpart = part;
                        Tpart = Tpart.Replace("{", "");
                        Tpart = Tpart.Replace("}", "");
                        Tpart = Tpart.Replace("[", "");
                        Tpart = Tpart.Replace("]", "");

                        string name = Tpart.Split("\"")[1];
                        string[] shareIds = Tpart.Split(":")[1].Split(",");
                        foreach(string shareid in shareIds)
                        {
                            ShareIds.Add(shareid);
                            Plugin.Log(3,"Shareid: " +  shareid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.Message);
                    return;
                }
            }
            // Got All Sharecodes - request Information now
            if (ShareIds.Count == 0) return;

            Plugin.Log(3, "Sharecodes received. Requesting Shocker Information...");

            string URL = $"https://ps.pishock.com/PiShock/GetShockersByShareIds?UserId={UserID}&Token={apikey}&api=true";
            foreach(string shareId in ShareIds)
            {
                URL += $"&shareIds={shareId}";
            }

            Result = await HttpClient.GetAsync(URL);
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Shared Shocker Information from Pishock.");
                return;
            }

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    //Plugin.Log(message);

                    string[] parts = message.Split("],");
                    foreach (string part in parts)
                    {
                        string Tpart = part;
                        string name = Tpart.Split("\"")[1];
                        string information = Tpart.Substring(name.Length + 3);
                        if(information.StartsWith(":")) information =  information.Substring(1);
                        if (information.EndsWith("}]}")) information = information.Replace("}]}","}");
                        information += "]";

                        SharedResponse[] test = JsonConvert.DeserializeObject<SharedResponse[]>(information)!;
                        foreach (SharedResponse response in test)
                        {
                            Plugin.Log(3,response);
                            if(name.ToLower().Equals(Plugin.Authentification.PishockName.ToLower()))continue;
                            ShockerPishock shocker = new ShockerPishock(response.shockerName, response.clientId, response.shockerId);
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
                    Plugin.Error(ex.Message);
                    return;
                }
            }
        }

        public async void SendRequest(ShockOptions Options)
        {

            if(Client == null || Status != ConnectionStatusPishock.Connected) return;

            #region Validation
            if (Plugin.Authentification.PishockName.Length < 3
                || Plugin.Authentification.PishockApiKey.Length < 16)
            {
                Plugin.Log(3, " -> Aborted due to invalid Account Settings!");
                return;
            }

            if (Plugin.isFailsafeActive)
            {
                Plugin.Log(3, " -> Blocked request due to failsafe mode!");
                return;
            }

            if (!Options.Validate())
            {
                Plugin.Log(3, " -> Blocked due to invalid ShockOptions!");
                return;
            }
            
            if (Options.ShockersPishock.Count == 0)
            {
                Plugin.Log(3, " -> No Pishock Shockers assigned, discarding!");
                return;
            }
            #endregion

            if(Options.WarningMode != WarningMode.None)
            {
                ShockOptions warningOptions = new ShockOptions(Options);
                warningOptions.OpMode = OpMode.Vibrate;
                warningOptions.Intensity = 55;
                warningOptions.Duration = 1;
                string sendWarning = CommandPublish.Generate(warningOptions, Plugin, UserID, true);
                await Client.Send(sendWarning);
                int delay;
                switch (Options.WarningMode)
                {
                    case WarningMode.Short: delay = new Random().Next(3000, 5000); break;
                    case WarningMode.Medium: delay = new Random().Next(7000, 12000); break;
                    case WarningMode.Long: delay = new Random().Next(12000, 27000); break;
                    default: delay = 2000; break;
                }
                Plugin.Log(3,"Warning sent!\nWaiting " + delay + "ms...");
                await Task.Delay(delay);
            }

            string sendCommand = CommandPublish.Generate(Options, Plugin, UserID, null);
            await Client.Send(sendCommand);
            Plugin.Log(3,"Command sent!");
        }




    }
}
