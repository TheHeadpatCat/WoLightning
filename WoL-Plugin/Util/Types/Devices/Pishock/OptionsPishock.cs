using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Util.Types.Devices.Pishock
{
    public class OptionsPishock : DeviceOptions
    {
        public override int Intensity_MIN_SUPPORTED { get; } = 1;
        public override int Intensity_MAX_SUPPORTED { get; } = 100;
        public override int Duration_MIN_SUPPORTED { get; } = 100;
        public override int Duration_MAX_SUPPORTED { get; } = 1500;

    }
}
