using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace WoLightning.WoL_Plugin.Clients.Buttplug
{
    [Serializable]
    public class IntifaceDevice
    {
        ButtplugClientDevice t;

        public uint Index { get; set; }
        public string Name { get; set; } = "Unknown";
        public string DisplayName { get; set; } = "Unknown";

        public IntifaceDevice(uint index, string name, string displayName)
        {
            Index = index;
            Name = name;
            DisplayName = displayName;
        }
    }
}
