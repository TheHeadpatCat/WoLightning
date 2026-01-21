using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Clients.Intiface;
using WoLightning.Clients.OpenShock;
using WoLightning.Clients.Pishock;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Clients.Webserver;
using WoLightning.WoL_Plugin.Util.Types.Devices.Intiface;
using WoLightning.WoL_Plugin.Util.Types.Devices.OpenShock;
using WoLightning.WoL_Plugin.Util.Types.Devices.Pishock;

namespace WoLightning.WoL_Plugin.Clients
{
    public class ClientController : IDisposable
    {
        private readonly Plugin Plugin;
        public bool IsSetup { get; private set; } = false;
        public ClientPishock? Pishock { get; private set; }
        public ClientOpenShock? OpenShock { get; private set; }
        public ClientIntiface? Intiface { get; private set; }



        // public ClientWebserver? Webserver { get; private set; }

        public ClientController(Plugin plugin) {
            Plugin = plugin;
        }

        public void Setup()
        {
            if (IsSetup) return;

            Pishock = new(Plugin);
            OpenShock = new(Plugin);
            Intiface = new(Plugin);

            IsSetup = true;
            ConnectAll();
        }

        public void ConnectAll()
        {
            if (!IsSetup) return;
            if (Plugin == null || Plugin.Authentification == null) return;

            if (Plugin.Authentification.PishockEnabled) Pishock.Setup();
            if (Plugin.Authentification.OpenShockEnabled) OpenShock.Setup();
            if (Plugin.Authentification.IntifaceEnabled) Intiface.Setup();
        }

        public void SendRequest(IdOptionPair[] Pairs)
        {
            if (!IsSetup) return;

            List<DeviceOptionPairPishock> pairsPishock = new();
            List<DeviceOptionPairOpenShock> pairsOpenShock = new();
            List<DeviceOptionPairIntiface> pairsIntiface = new();

            foreach (var pair in Pairs)
            {
                switch (pair.Device.Type)
                {
                    case Util.Types.DeviceType.Pishock:
                        pairsPishock.Add(new((ShockerPishock)pair.Device,(OptionsPishock)pair.Options));
                        break;
                    case Util.Types.DeviceType.OpenShock:
                        pairsOpenShock.Add(new((ShockerOpenShock)pair.Device))
                        break;
                    case Util.Types.DeviceType.Intiface:
                        break;
                }
            }
        }

        public void Dispose()
        {
            Pishock?.Dispose();
            OpenShock?.Dispose();
            Intiface?.Dispose();
        }
    }
}
