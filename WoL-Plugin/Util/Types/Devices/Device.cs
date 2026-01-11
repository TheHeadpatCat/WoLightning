using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types.Devices;

namespace WoLightning.WoL_Plugin.Util.Types
{
    public enum DeviceType
    {
        Unknown = -1,

        Pishock = 1,
        OpenShock = 2,
        Intiface = 3,
    }

    public enum DeviceStatus
    {
        Unchecked = 0,
        Uncheckable = 1,

        Checking = 99,
        
        Online = 200,
        Offline = 201,
        Paused = 202,

        Unreachable = 400,
        NotAuthorized = 401,
        NonExistant = 402,
        
        FatalError = 500,

    }

    [Flags]
    public enum DeviceCapability
    {
        Stop = 1,

        Shock = 2,
        Beep = 4,
        Vibrate = 8,

        Rotate = 16,
        Linear = 32,
        Oscillate = 64,
        Scalar = 128,
    }

    [Serializable]
    public abstract class Device
    {
        public Guid Id { get; set; } = new();
        public string Name { get; init; } = "Unknown";
        [JsonIgnore] public virtual DeviceStatus Status { get; set; } = DeviceStatus.Unchecked;
        public abstract DeviceType Type { get; }
        public abstract DeviceCapability Capabilities { get; }

        public Cooldown Cooldown { get; } = new();

        public Action<DeviceOptions> Triggered;

        public Device()
        {
            
        }

        public override string ToString()
        {
            return $"[Device-{Type}] ID: {Id} Status: {Status} Cooldown: {(Cooldown.HasCooldown ? Cooldown.ToString() : "None." )}";
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is not Device) return false;

            Device other = (Device)obj;
            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
