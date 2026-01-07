using Buttplug.Client;
using System;

namespace WoLightning.WoL_Plugin.Clients.Buttplug
{
    [Serializable]
    public class DeviceIntiface
    {
        ButtplugClientDevice t;

        public uint Index { get; set; }
        public string Name { get; set; } = "Unknown";
        public string DisplayName { get; set; } = "Unknown";

        public DeviceIntiface(uint index, string name, string displayName)
        {
            Index = index;
            Name = name;
            DisplayName = displayName;
        }
    }
}
