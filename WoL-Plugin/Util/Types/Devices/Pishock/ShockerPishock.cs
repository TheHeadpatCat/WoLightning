using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Clients.Pishock
{

    [Serializable]
    public class ShockerPishock : Device
    {
        public override DeviceType Type { get; } = DeviceType.Pishock;
        public override DeviceCapability Capabilities { get; } = DeviceCapability.Shock | DeviceCapability.Vibrate | DeviceCapability.Beep;

        public int ClientId { get; init; } = -1;
        public int ShockerId { get; init; } = -1;
        [JsonIgnore] public bool IsPaused { get; init; } = false;
        public bool IsPersonal { get; init; } = true;
        public int ShareId { get; init; } = -1;
        public string ShareCode { get; init; } = "";
        public string OwnerUsername { get; init; } = "";
    

        public ShockerPishock(string name, int clientId, int shockerId) : base(name)
        {
            Name = name;
            ClientId = clientId;
            ShockerId = shockerId;
        }

        public override string ToString()
        {
            return $"{base.ToString()}\n -> [ShockerPishock] ShockerId: {ShockerId} ClientId: {ClientId}";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is ShockerPishock)
            {
                ShockerPishock other = (ShockerPishock)obj;
                return ClientId == other.ClientId && ShockerId == other.ShockerId && ShareId == other.ShareId && ShareCode == other.ShareCode;
            }
            return false;
        }

    }
}
