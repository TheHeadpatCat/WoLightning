using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using WoLightning.WoL_Plugin.Clients.OpenShock;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.OpenShock
{
    [Serializable]
    public class ShockerOpenShock : Device
    {
        public override DeviceType Type { get; } = DeviceType.OpenShock;
        public override DeviceCapability Capabilities { get; } = DeviceCapability.Shock | DeviceCapability.Vibrate | DeviceCapability.Beep;

        public string ShockerId { get; init; } = "";
        public bool IsPaused { get; set; } = false;
        internal HubOpenShock ParentHub;

        public ShockerOpenShock(string name, string shockerId) : base(name)
        {
            ShockerId = shockerId;
        }

        internal ShockerOpenShock(HubOpenShock parent, string name, string shockerId) : this(name, shockerId)
        {
            ParentHub = parent;
        }

        public override string ToString()
        {
            return $"{base.ToString()}\n -> [ShockerOpenShock] ShockerId: {ShockerId} Paused: {IsPaused} Parent: {ParentHub.ToString()}";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is ShockerOpenShock)
            {
                ShockerOpenShock other = (ShockerOpenShock)obj;
                return ShockerId == other.ShockerId && Name == other.Name;
            }
            return false;
        }

    }
}
