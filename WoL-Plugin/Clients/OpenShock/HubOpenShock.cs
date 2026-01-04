using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Clients.OpenShock
{
    [Serializable]
    internal class HubOpenShock : IDisposable
    {
        public enum ConnectionStatusOpenShockHub
        {
            NotStarted = 0,
            Unavailable = 1,
            InvalidUserdata = 2,
            FatalError = 3,

            Connecting = 99,
            ConnectedNoInfo = 100,
            Connected = 200,
        }

        private readonly Plugin Plugin;
        public string Gateway { get; set; }
        public string Country { get; set; }
        public string DeviceId { get; set; }

        public ConnectionStatusOpenShockHub Status { get; set; } = ConnectionStatusOpenShockHub.NotStarted;

        public HubOpenShock(Plugin plugin, string deviceId)
        {
            this.Plugin = plugin;
            this.DeviceId = deviceId;
        }

        public void Dispose()
        {

        }

        public async Task Setup()
        {
            await RequestAllShockers();
        }

        private async Task RequestAllShockers()
        {
            if (Plugin == null || Plugin.Authentification == null) return;
            string apikey = Plugin.Authentification.OpenShockApiKey, url = Plugin.Authentification.OpenShockURL;

            Logger.Log(3, "Requesting OpenShock Device Shockers for " + DeviceId + "...");

            HttpResponseMessage Result;

            try
            {
                HttpClient HttpClient = new();
                HttpClient.DefaultRequestHeaders.Add("Open-Shock-Token", apikey);
                HttpClient.DefaultRequestHeaders.Add("User-Agent", "WoLightning Plugin");
                Result = await HttpClient.GetAsync($"{url}/1/devices/{DeviceId}/shockers");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error("Something went wrong while fetching OpenShock Shocker Data for " + DeviceId);
                Status = ConnectionStatusOpenShockHub.FatalError;
                return;
            }

            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Logger.Error("Could not retrieve Device Shockers from " + DeviceId);
                Status = ConnectionStatusOpenShockHub.Unavailable;
                Logger.Log(1, new StreamReader(Result.Content.ReadAsStream()).ReadToEnd());
                return;
            }
            try
            {

                using (var reader = new StreamReader(Result.Content.ReadAsStream()))
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    Logger.Log(3, message);
                    ResponseDeviceShockers test = JsonConvert.DeserializeObject<ResponseDeviceShockers>(message)!;
                    Logger.Log(3, test);
                    foreach (var shocker in test.data)
                    {
                        ShockerOpenShock ShockerT = new(this, shocker.name, shocker.id, shocker.isPaused);
                        Logger.Log(3, ShockerT);
                        Plugin.Authentification.OpenShockShockers.Add(ShockerT);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Logger.Error("Something went wrong while reading OpenShock Device Shocker data from " + DeviceId);
                Status = ConnectionStatusOpenShockHub.FatalError;
                return;
            }
        }

    }
}
