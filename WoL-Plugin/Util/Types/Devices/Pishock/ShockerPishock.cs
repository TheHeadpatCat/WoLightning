using System;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Clients.Pishock
{

    [Serializable]
    public class ShockerPishock : Device
    {
        public override DeviceType Type { get; } = DeviceType.Pishock;
        public override DeviceCapability Capabilities { get; } = DeviceCapability.Shock | DeviceCapability.Vibrate | DeviceCapability.Vibrate;

        public string Name { get; init; } = "Unknown";
        public int ClientId { get; init; } = -1;
        public int ShockerId { get; init; } = -1;
        public bool IsPaused { get; init; } = false;
        public bool IsPersonal { get; init; } = true;
        public int ShareId { get; init; } = -1;
        public string ShareCode { get; init; } = "";
        public string Username { get; init; } = "";

    

        public ShockerPishock(string name, int clientId, int shockerId) : base(ShockerType.Pishock, name)
        {
            this.clientId = clientId;
            this.shockerId = shockerId;
        }

        override public string getInternalId()
        {
            return Type + "#" + name + "#" + shockerId;
        }

        public override string ToString()
        {
            return "[ShockerPishock] Name: " + name + " ShockerId: " + shockerId + " ClientId: " + clientId;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is ShockerPishock)
            {
                ShockerPishock other = (ShockerPishock)obj;
                return clientId == other.clientId && shockerId == other.shockerId && shareId == other.shareId && shareCode == other.shareCode;
            }
            return false;
        }

    }
}
