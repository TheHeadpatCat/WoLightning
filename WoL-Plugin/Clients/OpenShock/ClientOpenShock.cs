using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.OpenShock;



namespace WoLightning.Clients.OpenShock
{
    public class ClientOpenShock : IDisposable
    {
        public enum ConnectionStatusOpenShock
        {
            NotStarted = 0,
            Unavailable = 1,
            InvalidUserdata = 2,
            FatalError = 3,

            Connecting = 99,
            ConnectedNoInfo = 100,
            ConnectedNoDevices = 101,
            Connected = 200,
        }

        private Plugin? Plugin;
        public ConnectionStatusOpenShock Status { get; set; } = ConnectionStatusOpenShock.NotStarted;
        private List<HubOpenShock> Devices = new();
        public string UserId;
        public string Username;
        public HttpClient Client;

        public ClientOpenShock(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {
            foreach (var device in Devices)
            {
                device.Dispose();
            }
        }

        public async void Setup()
        {
            await SetupAllData();
        }

        public async Task SetupAllData()
        {
            if (Plugin == null || Plugin.Authentification == null) return;
            Plugin.Authentification.OpenShockShockers.Clear();
            Devices.Clear();

            if (Client != null) Client.Dispose();

            string apikey = Plugin.Authentification.OpenShockApiKey;
            if (apikey == null || apikey.Length < 3) return;

            Client = new();
            Client.DefaultRequestHeaders.Add("Open-Shock-Token", apikey);
            Client.DefaultRequestHeaders.Add("User-Agent", "WoLightning Plugin");

            await RequestAccountInformation();
            if (Status != ConnectionStatusOpenShock.ConnectedNoInfo) return;

            Status = ConnectionStatusOpenShock.Connected;

            await RequestDevices();

            if (Devices.Count == 0)
            {
                Status = ConnectionStatusOpenShock.ConnectedNoDevices;
                return;
            }

            foreach (var device in Devices)
            {
                await device.Setup();
            }

            Plugin.Configuration.ActivePreset.ValidateShockers();
        }

        private async Task RequestAccountInformation()
        {
            if (Plugin == null || Plugin.Authentification == null) return;
            string apikey = Plugin.Authentification.OpenShockApiKey, url = Plugin.Authentification.OpenShockURL;

            Plugin.Log("Requesting OpenShock Account information...");

            HttpResponseMessage Result;

            try
            {
                Result = await Client.GetAsync($"{url}/1/users/self");
            }
            catch(Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Something went wrong while fetching OpenShock Account Data.");
                Status = ConnectionStatusOpenShock.FatalError;
                return;
            }

            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Account Information from OpenShock.");
                Status = ConnectionStatusOpenShock.Unavailable;
                Plugin.Log(new StreamReader(Result.Content.ReadAsStream()).ReadToEnd());
                return;
            }
            try
            {

                using (var reader = new StreamReader(Result.Content.ReadAsStream()))
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    ResponseAccount test = JsonConvert.DeserializeObject<ResponseAccount>(message)!;
                    UserId = test.data.id;
                    Username = test.data.name;
                }
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Something went wrong while reading OpenShock Account Data.");
                Status = ConnectionStatusOpenShock.FatalError;
                return;
            }
            if(UserId.Length > 1) Status = ConnectionStatusOpenShock.ConnectedNoInfo;
            else Status = ConnectionStatusOpenShock.InvalidUserdata;
        }

        private async Task RequestDevices()
        {
            string apikey = Plugin.Authentification.OpenShockApiKey, url = Plugin.Authentification.OpenShockURL;

            Plugin.Log("Requesting OpenShock Device Information...");

            var Result = await Client.GetAsync($"{url}/1/devices");
            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Devices from OpenShock.");
                Status = ConnectionStatusOpenShock.Unavailable;
                return;
            }

            Plugin.Log(" -> Received OpenShock Device Information!");

            using (var reader = new StreamReader(Result.Content.ReadAsStream()))
            {
                try
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    ResponseDevices devices = JsonConvert.DeserializeObject<ResponseDevices>(message)!;
                    Plugin.Log(devices);
                    foreach(var device in devices.data)
                    {
                        Devices.Add(new HubOpenShock(Plugin,device.id));
                    }

                }
                catch (Exception ex)
                {
                    Plugin.Error(ex.Message);
                }
            }
        }
        
        public async void SendRequest(ShockOptions Options)
        {

            if (Status != ConnectionStatusOpenShock.Connected) return;

            #region Validation

            if (Plugin.isFailsafeActive)
            {
                Plugin.Log(" -> Blocked request due to failsafe mode!");
                return;
            }

            if (!Options.Validate())
            {
                Plugin.Log(" -> Blocked due to invalid ShockOptions!");
                return;
            }

            if (Options.ShockersOpenShock.Count == 0)
            {
                Plugin.Log(" -> No OpenShock Shockers assigned, discarding!");
                return;
            }
            #endregion

            try
            {

                if (Options.WarningMode != WarningMode.None)
                {
                    ShockOptions warningOptions = new ShockOptions(Options);
                    warningOptions.OpMode = OpMode.Vibrate;
                    warningOptions.Intensity = 55;
                    warningOptions.Duration = 1;
                    foreach (var shocker in Options.ShockersOpenShock)
                    {
                        Plugin.Log("Sending Warning.");
                        StringContent jsonContentWarn = new(CommandPublish.Generate(Options.ShockersOpenShock, warningOptions), Encoding.UTF8, "application/json");
                        await Client.PostAsync($"{Plugin.Authentification.OpenShockURL}/2/shockers/control", jsonContentWarn);
                    }
                    Plugin.Log("Warnings sent!");
                    int delay;
                    switch (Options.WarningMode)
                    {
                        case WarningMode.Short: delay = new Random().Next(3000, 5000); break;
                        case WarningMode.Medium: delay = new Random().Next(7000, 12000); break;
                        case WarningMode.Long: delay = new Random().Next(12000, 27000); break;
                        default: delay = 2000; break;
                    }
                    await Task.Delay(delay);
                }

                Plugin.Log("Sending Command");
                StringContent jsonContent = new(CommandPublish.Generate(Options.ShockersOpenShock, Options), Encoding.UTF8, "application/json");
                var result = await Client.PostAsync($"{Plugin.Authentification.OpenShockURL}/2/shockers/control", jsonContent);
                Plugin.Log(new StreamReader(result.Content.ReadAsStream()).ReadToEnd());
            }
            catch(Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Failed to send Command to OpenShock Shocker");
            }
        }


        /*
        private int sendRemainingCommands(ShockerOpenShock shocker,string sendCommand,int amountRemaining)
        {
            if (amountRemaining <= 0) return 0;
            Plugin.Log(sendCommand);
            shocker.ParentHub.Client.Send(sendCommand);
            await Task.Delay(500);
            sendRemainingCommands(shocker,sendCommand,amountRemaining -1);
        }
        */


    }
}
