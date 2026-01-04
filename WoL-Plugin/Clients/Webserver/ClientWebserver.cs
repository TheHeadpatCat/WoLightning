using System;
using System.Net.Http;
using WoLightning.Clients.Webserver;

namespace WoLightning.WoL_Plugin.Clients.Webserver
{
    public class ClientWebserver : IDisposable
    {
        private readonly Plugin Plugin;

        public ConnectionStatusWebserver Status = ConnectionStatusWebserver.NotStarted;

        readonly HttpClient HttpClient;
        private readonly Uri Address = new Uri("wss://localhost:7149");


        public string ServerVersion = string.Empty;

        public ClientWebserver(Plugin plugin)
        {
            Plugin = plugin;
        }
        public void Dispose()
        {

        }
    }
}
