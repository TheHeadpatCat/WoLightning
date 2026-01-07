using Buttplug.Client;
using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Clients.Intiface
{
    [Serializable]
    public class DeviceIntiface
    {
        public uint Index { get; set; } = 999;
        public string Name { get; set; } = "Unknown";
        public string DisplayName { get; set; } = "Unknown";

        public DeviceIntiface() { }

        public DeviceIntiface(uint index, string name, string displayName)
        {
            Index = index;
            Name = name;
            DisplayName = displayName;
        }

        public DeviceIntiface(ButtplugClientDevice device)
        {
            Index = device.Index;
            Name = device.Name;
            DisplayName = device.DisplayName;
        }
    }
}
