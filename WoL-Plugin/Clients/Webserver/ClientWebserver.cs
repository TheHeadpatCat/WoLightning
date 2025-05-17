using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Clients.Webserver;

namespace WoLightning.WoL_Plugin.Clients.Webserver
{
    public class ClientWebserver : IDisposable
    {
        private readonly Plugin Plugin;

        public ConnectionStatusWebserver Status = ConnectionStatusWebserver.NotStarted;

        HttpClient HttpClient;
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
