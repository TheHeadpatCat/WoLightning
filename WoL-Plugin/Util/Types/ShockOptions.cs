
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Clients;
using WoLightning.WoL_Plugin.Clients.Intiface;
using WoLightning.WoL_Plugin.Clients.OpenShock;
using WoLightning.WoL_Plugin.Clients.Pishock;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.Util.Types
{
    public enum OpMode
    {
        Shock = 0,
        Vibrate = 1,
        Beep = 2
    }

    public enum CooldownModifier
    {
        Miliseconds = 100,
        Seconds = 1000,
        Minutes = 60000,
        Hours = 3600000,
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
    public class ShockOptions
    {

        public bool isEnabled { get; set; } = false;

        public List<ShockerPishock> ShockersPishock { get; set; } = new();
        public List<ShockerOpenShock> ShockersOpenShock { get; set; } = new();
        public List<DeviceIntiface> DevicesIntiface { get; set; } = new();

        public OpMode OpMode { get; set; } = OpMode.Shock;
        public int Intensity { get; set; } = 1;
        public int Duration { get; set; } = 1;
        public double Cooldown { get; set; } = 0;
        public CooldownModifier modifier { get; set; } = CooldownModifier.Seconds;

        public WarningMode WarningMode { get; set; } = WarningMode.None;


        // Randomization
        public bool isIntensityRandomized { get; set; } = false;
        public int RandomizeIntensityMin { get; set; } = 1;
        public bool isDurationRandomized { get; set; } = false;
        public int RandomizeDurationMin { get; set; } = 1;

        [NonSerialized] public bool isModalOpen = true; // Used for Configwindow
        [NonSerialized] public bool isOptionsOpen = false;
        [NonSerialized] public TimerPlus CooldownTimer = new();
        [NonSerialized] public bool hasBeenReset = false;



        public ShockOptions()
        {
            CooldownTimer.AutoReset = false;
        }
        public ShockOptions(int Mode, int Intensity, int Duration)
        {
            this.OpMode = (OpMode)Mode;
            this.Intensity = Intensity;
            this.Duration = Duration;
            CooldownTimer.AutoReset = false;
        }

        public ShockOptions(OpMode Mode, int Intensity, int Duration)
        {
            this.OpMode = Mode;
            this.Intensity = Intensity;
            this.Duration = Duration;
            CooldownTimer.AutoReset = false;
        }
        public ShockOptions(int[] Settings)
        {
            this.OpMode = (OpMode)Settings[0];
            this.Intensity = Settings[1];
            this.Duration = Settings[2];
            CooldownTimer.AutoReset = false;
        }

        [JsonConstructor]
        public ShockOptions(bool isEnabled, List<ShockerPishock> shockersPishock, List<ShockerOpenShock> shockersOpenShock, List<DeviceIntiface> devicesIntiface, OpMode opMode, int intensity, int duration, double cooldown, CooldownModifier modifier, WarningMode warningMode, bool isIntensityRandomized, int randomizeIntensityMin, bool isDurationRandomized, int randomizeDurationMin, bool isModalOpen, bool isOptionsOpen, TimerPlus cooldownTimer, bool hasBeenReset)
        {
            this.isEnabled = isEnabled;
            ShockersPishock = shockersPishock;
            ShockersOpenShock = shockersOpenShock;
            DevicesIntiface = devicesIntiface;
            OpMode = opMode;
            Intensity = intensity;
            Duration = duration;
            Cooldown = cooldown;
            this.modifier = modifier;
            WarningMode = warningMode;
            this.isIntensityRandomized = isIntensityRandomized;
            RandomizeIntensityMin = randomizeIntensityMin;
            this.isDurationRandomized = isDurationRandomized;
            RandomizeDurationMin = randomizeDurationMin;
            this.isModalOpen = isModalOpen;
            this.isOptionsOpen = isOptionsOpen;
            CooldownTimer = cooldownTimer;
            this.hasBeenReset = hasBeenReset;
        }
        public ShockOptions(ShockOptions other)
        {
            this.isEnabled = other.isEnabled;
            ShockersPishock = other.ShockersPishock;
            ShockersOpenShock = other.ShockersOpenShock;
            DevicesIntiface = other.DevicesIntiface;
            OpMode = other.OpMode;
            Intensity = other.Intensity;
            Duration = other.Duration;
            Cooldown = other.Cooldown;
            this.modifier = other.modifier;
            WarningMode = other.WarningMode;
            this.isIntensityRandomized = other.isIntensityRandomized;
            RandomizeIntensityMin = other.RandomizeIntensityMin;
            this.isDurationRandomized = other.isDurationRandomized;
            RandomizeDurationMin = other.RandomizeDurationMin;
            this.isModalOpen = other.isModalOpen;
            this.isOptionsOpen = other.isOptionsOpen;
            CooldownTimer = other.CooldownTimer;
            this.hasBeenReset = other.hasBeenReset;
        }

        public bool Validate()
        {
            if ((int)OpMode < 0 || (int)OpMode > 2) return false;                       // Check for correct OpMode
            if (Intensity < 1) Intensity = 1;                                          // Clamp Intesity Min
            if (Intensity > 100) Intensity = 100;                                     // Clamp Intensity Max
            if (Duration < 1 || (Duration > 10 && Duration < 100)) Duration = 100;   // Clamp Duration Min
            if (Duration > 10 && Duration != 100 && Duration != 300) Duration = 10; //Clamp Duration Max
            if (modifier != CooldownModifier.Miliseconds && modifier != CooldownModifier.Seconds && modifier != CooldownModifier.Minutes && modifier != CooldownModifier.Hours) modifier = CooldownModifier.Seconds;
            return true;
        }
        public bool IsEnabled()
        {
            return isEnabled;
        }

        public bool CanExecute()
        {
            return isEnabled && ShockersPishock.Count > 0;
        }



        public override string ToString()
        {
            return $"[ShockOptions] Mode:{OpMode} {Intensity}%|{Duration}s Cooldown:{Cooldown * (int)modifier}ms Applied to {ShockersPishock.Count} Shockers.";
        }

        public int[] toSimpleArray()
        {
            return [(int)OpMode, Intensity, Duration];
        }

        #region Util


        public string getOpModePishock()
        {
            switch (OpMode)
            {
                case OpMode.Shock: return "s";
                case OpMode.Vibrate: return "v";
                case OpMode.Beep: return "b";
                default: return "e";

            }
        }

        public string getOpModeOpenShock()
        {
            switch (OpMode)
            {
                case OpMode.Shock: return "shock";
                case OpMode.Vibrate: return "vibrate";
                case OpMode.Beep: return "sound";
                default: return "stop";

            }
        }

        public int getShockerCount()
        {
            return ShockersPishock.Count + ShockersOpenShock.Count + DevicesIntiface.Count;
        }

        public List<ShockerBase> GetShockers()
        {
            List<ShockerBase> shockers = new();
            shockers.AddRange(ShockersPishock);
            shockers.AddRange(ShockersOpenShock);
            return shockers;
        }

        public int getDurationOpenShock()
        {
            int output = Duration;

            if (output <= 10) return output * 1000;
            if (output < 300) return 300;
            if (output >= 10000) return 10000;
            return output;
        }

        public string durationString()
        {
            string output = "";
            switch (Duration) { case 100: output = "0.1s"; break; case 300: output = "0.3s"; break; default: output = Duration + "s"; break; }
            return output;
        }

        public string getShockerNames()
        {
            string output = "";
            foreach (var shocker in ShockersPishock) output += shocker.name + ", ";
            return output;
        }

        public string getShockerNamesNewLine()
        {
            string output = "";
            foreach (var shocker in ShockersPishock) output += shocker.name + "\n";
            return output;
        }

        public ShockOptions Clone()
        {
            return (MemberwiseClone() as ShockOptions)!;
        }

        public bool hasCooldown()
        {
            return CooldownTimer.TimeLeft > 0;
        }

        public int cooldownLeft()
        {
            return CooldownTimer.TimeLeftSeconds;
        }
        public void startCooldown()
        {
            double lowDurMult = 1;
            if (Duration > 10) lowDurMult = 0.01;
            double CooldownTime = Cooldown * (int)modifier + Duration * 1000 * lowDurMult + 1000;
            CooldownTimer.Start(CooldownTime);
            Logger.Log(4, "CD start: " + CooldownTime);
        }
        #endregion


    }
}
