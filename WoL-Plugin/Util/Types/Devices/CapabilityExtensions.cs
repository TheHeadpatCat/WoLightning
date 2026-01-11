using System;
using System.Collections.Generic;
using System.Text;

namespace WoLightning.WoL_Plugin.Util.Types.Devices
{
    public static class CapabilityExtensions
    {
        public static bool HasAny(this DeviceCapability current, DeviceCapability other)
        {
            return current.HasFlag(other);
        }

        public static bool HasAll(this DeviceCapability current, DeviceCapability other)
        {
            return current == other; // todo: this method is wrong
        }
    }
}
