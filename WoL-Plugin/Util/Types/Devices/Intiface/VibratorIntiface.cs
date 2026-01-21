using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.Intiface
{
    [Serializable]
    public class VibratorIntiface : Device
    {
        public override DeviceType Type { get; } = DeviceType.Intiface;

        public override DeviceCapability Capabilities { get; } = DeviceCapability.Stop | DeviceCapability.Vibrate | DeviceCapability.Rotate | DeviceCapability.Linear | DeviceCapability.Oscillate | DeviceCapability.Scalar;
        public override bool SupportsPatterns { get; } = true;

        public uint Index { get; init; } = 999;
        [JsonIgnore] public string DisplayName { get; init; } = "";

        public VibratorIntiface(string name) : base(name)
        {
        }

        public VibratorIntiface(string name, uint index, string displayName) : this(name)
        {
            Index = index;
            DisplayName = displayName;
        }

        public VibratorIntiface(ButtplugClientDevice device) : this(device.Name)
        {
            Index = device.Index;
            DisplayName = device.DisplayName;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is VibratorIntiface)
            {
                VibratorIntiface other = (VibratorIntiface)obj;
                return Index == other.Index && Name == other.Name && DisplayName == other.DisplayName;
            }
            return false;
        }

        

    }
}
