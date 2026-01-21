
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.Intiface;
using WoLightning.WoL_Plugin.Clients.OpenShock;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.Util.Types
{
    public enum OpMode
    {
        Shock = 0,
        Vibrate = 1,
        Beep = 2
    }

    public enum WarningMode
    {
        None = 0,
        Short = 1,
        Medium = 2,
        Long = 3,
        Random = 4
    }

    [Serializable]
    public abstract class DeviceOptions
    {
        public DeviceCapability Operation { get; set; } = DeviceCapability.None;

        public int Intensity { get; set; } // Use as standard
        public int IntensityMax { get; set; } // Max is only in use if randomization is on.
        public abstract int Intensity_MIN_SUPPORTED { get; }
        public abstract int Intensity_MAX_SUPPORTED { get; }


        public int Duration { get; set; }
        public int DurationMax { get; set; }
        public abstract int Duration_MIN_SUPPORTED { get; }
        public abstract int Duration_MAX_SUPPORTED { get; }

        public Cooldown Cooldown { get; } = new();

        public WarningMode WarningMode { get; set; } = WarningMode.None;

        public DeviceOptions()
        {
            Intensity = Intensity_MIN_SUPPORTED;
            Duration = Duration_MIN_SUPPORTED;
        }
        public DeviceOptions(DeviceCapability operation) : this()
        {
            Operation = operation;
        }

        public DeviceOptions(DeviceCapability operation, int intensity, int duration) : this(operation)
        {
            Intensity = Math.Clamp(intensity, Intensity_MIN_SUPPORTED, Intensity_MAX_SUPPORTED);
            Duration = Math.Clamp(duration, Duration_MIN_SUPPORTED, Duration_MAX_SUPPORTED);
        }

        public DeviceOptions(DeviceOptions other)
        {
            Operation = other.Operation;
            Intensity = Math.Clamp(other.Intensity, Intensity_MIN_SUPPORTED, Intensity_MAX_SUPPORTED);
            IntensityMax = Math.Clamp(other.IntensityMax, Intensity_MIN_SUPPORTED, Intensity_MAX_SUPPORTED);
            Duration = Math.Clamp(other.Duration, Duration_MIN_SUPPORTED, Duration_MAX_SUPPORTED);
            DurationMax = Math.Clamp(other.DurationMax, Duration_MIN_SUPPORTED, Duration_MAX_SUPPORTED);
        }


        public abstract string OperationString();


        public virtual bool Validate()
        {
            if (Operation == DeviceCapability.None) return false;
            Intensity = Math.Clamp(Intensity, 1, Intensity_MAX_SUPPORTED);
            Duration = Math.Clamp(Duration, Duration_MIN_SUPPORTED, Duration_MAX_SUPPORTED);
            return true;
        }

        public override string ToString()
        {
            return $"[DeviceOptions] Operation: {Operation} {Intensity}%|{Duration}ms";
        }


    }


    public struct IdOptionPair
    {
        public Guid Id;
        public DeviceOptions Options;
    }

    public struct DeviceOptionPair
    {
        public Device Device;
        public DeviceOptions Options;
    }
}
