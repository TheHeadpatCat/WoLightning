using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.Intiface
{
    [Serializable]
    public class OptionsIntiface : DeviceOptions
    {
        public override int Intensity_MIN_SUPPORTED { get; } = 1;
        public override int Intensity_MAX_SUPPORTED { get; } = 100;
        public override int Duration_MIN_SUPPORTED { get; } = 100;
        public override int Duration_MAX_SUPPORTED { get; } = 1500;

        public override string OperationString()
        {
            switch (Operation)
            {
                case DeviceCapability.Stop: return "Stop";
                case DeviceCapability.Vibrate: return "Vibrate";
                case DeviceCapability.Rotate: return "Rotate";
                case DeviceCapability.Linear: return "Linear";
                case DeviceCapability.Oscillate: return "Oscillate";
                case DeviceCapability.Scalar: return "Scalar";
                default: throw new Exception("OptionsIntiface has Invalid Operation assigned: " + Operation);
            }
        }

    }

    public struct DeviceOptionPairIntiface
    {
        public VibratorIntiface Device;
        public OptionsIntiface Options;
    }
}
