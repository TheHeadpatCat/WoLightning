using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types.Devices.Intiface;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.OpenShock
{
    [Serializable]
    public class OptionsOpenShock : DeviceOptions
    {
        public override int Intensity_MIN_SUPPORTED { get; } = 1;
        public override int Intensity_MAX_SUPPORTED { get; } = 100;
        public override int Duration_MIN_SUPPORTED { get; } = 100;
        public override int Duration_MAX_SUPPORTED { get; } = 1500;

        public override string OperationString()
        {
            switch (Operation)
            {
                case DeviceCapability.Shock: return "shock";
                case DeviceCapability.Vibrate: return "vibrate";
                case DeviceCapability.Beep: return "sound";
                case DeviceCapability.Stop: return "stop";
                default: throw new Exception("OptionsPishock has Invalid Operation assigned: " + Operation);
            }
        }
    }

    public struct DeviceOptionPairOpenShock
    {
        public ShockerOpenShock Device;
        public OptionsOpenShock Options;
    }
}



