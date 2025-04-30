using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Game.Rules;

namespace WoLightning.Clients.OpenShock
{
    public class ClientOpenShock : IDisposable
    {
        public enum ConnectionStatusOpenShock
        {
            NotStarted = 0,
            Unavailable = 1,

            Connecting = 199,
            Connected = 200,
        }


        private Plugin Plugin;
        public ConnectionStatusOpenShock Status { get; set; } = ConnectionStatusOpenShock.NotStarted;
        private HttpClient? Client;

        public ClientOpenShock(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {
            if (Client != null)
            {
                Client.CancelPendingRequests();
                Client.Dispose();
                Client = null;
            }
        }
        public void createHttpClient()
        {
            if (Client != null) return;

            Client = new HttpClient();
        }


        
    }
}
