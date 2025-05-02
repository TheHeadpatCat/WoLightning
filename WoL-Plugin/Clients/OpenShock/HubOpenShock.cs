using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static WoLightning.Clients.OpenShock.ClientOpenShock;

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

        private Plugin Plugin;
        public string Gateway {  get; set; }
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

            Plugin.Log("Requesting OpenShock Device Shockers for " + DeviceId + "...");

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
                Plugin.Error(ex.Message);
                Plugin.Error("Something went wrong while fetching OpenShock Shocker Data for " + DeviceId);
                Status = ConnectionStatusOpenShockHub.FatalError;
                return;
            }

            if (Result.StatusCode != HttpStatusCode.OK)
            {
                Plugin.Error("Could not retrieve Device Shockers from " + DeviceId);
                Status = ConnectionStatusOpenShockHub.Unavailable;
                Plugin.Log(new StreamReader(Result.Content.ReadAsStream()).ReadToEnd());
                return;
            }
            try
            {

                using (var reader = new StreamReader(Result.Content.ReadAsStream()))
                {
                    string message = reader.ReadToEnd();
                    if (message == null || message.Length == 0) return;
                    Plugin.Log(message);
                    ResponseDeviceShockers test = JsonConvert.DeserializeObject<ResponseDeviceShockers>(message)!;
                    Plugin.Log(test);
                    foreach(var shocker in test.data)
                    {
                        ShockerOpenShock ShockerT = new(this,shocker.name, shocker.id, shocker.isPaused);
                        Plugin.Log(ShockerT);
                        Plugin.Authentification.OpenShockShockers.Add(ShockerT);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.Message);
                Plugin.Error("Something went wrong while reading OpenShock Device Shocker data from " + DeviceId);
                Status = ConnectionStatusOpenShockHub.FatalError;
                return;
            }
        }

    }
}
