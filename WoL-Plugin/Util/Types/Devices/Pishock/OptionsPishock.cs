using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util.Types.Devices.OpenShock;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.Pishock
{
    public class OptionsPishock : DeviceOptions
    {
        public override int Intensity_MIN_SUPPORTED { get; } = 1;
        public override int Intensity_MAX_SUPPORTED { get; } = 100;
        public override int Duration_MIN_SUPPORTED { get; } = 100;
        public override int Duration_MAX_SUPPORTED { get; } = 1500;

        public override string OperationString()
        {
            switch (Operation)
            {
                case DeviceCapability.Shock: return "s";
                case DeviceCapability.Vibrate: return "v";
                case DeviceCapability.Beep: return "b";
                case DeviceCapability.Stop: return "e"; // todo: unknown what this request does, pishock docs dont say anything about it
                default: throw new Exception("OptionsPishock has Invalid Operation assigned: " + Operation);
            }
        }

    }

    public struct DeviceOptionPairPishock
    {
        public ShockerPishock Device;
        public OptionsPishock Options;

        public DeviceOptionPairPishock(ShockerPishock device, OptionsPishock options)
        {
            Device = device;
            Options = options;
        }
    }
}
