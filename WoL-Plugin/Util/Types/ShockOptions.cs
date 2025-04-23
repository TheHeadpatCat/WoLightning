using System;
using System.Collections.Generic;

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
        Miliseconds = 1000,
        Seconds = 10000,
        Minutes = 600000,
        Hours = 36000000,
    }

    [Serializable]
    public class ShockOptions
    {

        public bool isEnabled { get; set; } = false;
        public List<Shocker> Shockers { get; set; } = new(); // List of all Shockers to execute on

        public OpMode OpMode { get; set; } = OpMode.Shock;
        public int Intensity { get; set; } = 1;
        public int Duration { get; set; } = 1;
        public int Cooldown { get; set; } = 0;
        public CooldownModifier modifier { get; set; } = CooldownModifier.Seconds;


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
            CooldownTimer.Stop();
        }
        public ShockOptions(int Mode, int Intensity, int Duration)
        {
            this.OpMode = (OpMode)Mode;
            this.Intensity = Intensity;
            this.Duration = Duration;
            CooldownTimer.AutoReset = false;
            CooldownTimer.Stop();
        }
        public ShockOptions(int[] Settings)
        {
            this.OpMode = (OpMode)Settings[0];
            this.Intensity = Settings[1];
            this.Duration = Settings[2];
            CooldownTimer.AutoReset = false;
            CooldownTimer.Stop();
        }

        public bool Validate()
        {
            if ((int)OpMode < 0 || (int)OpMode > 2) return false;                       // Check for correct OpMode
            if (Intensity < 1) Intensity = 1;                                          // Clamp Intesity Min
            if (Intensity > 100) Intensity = 100;                                     // Clamp Intensity Max
            if (Duration < 1) Duration = 1;                                          // Clamp Duration Min
            if (Duration > 10 && Duration != 100 && Duration != 300) Duration = 10; //Clamp Duration Max
            return true;
        }
        public bool IsEnabled()
        {
            return isEnabled;
        }

        public bool CanExecute()
        {
            return isEnabled && Shockers.Count > 0;
        }



        public override string ToString()
        {
            return $"[ShockOptions] Mode:{OpMode} {Intensity}%|{Duration}s Cooldown:{Cooldown * (int)modifier}ms Applied to {Shockers.Count} Shockers.";
        }

        public int[] toSimpleArray()
        {
            return [(int)OpMode, Intensity, Duration];
        }

        #region Util
        public string durationString()
        {
            string output = "";
            switch (Duration) { case 100: output = "0.1s"; break; case 300: output = "0.3s"; break; default: output = Duration + "s"; break; }
            return output;
        }

        public string getShockerNames()
        {
            string output = "";
            foreach (var shocker in Shockers) output += shocker.Name + ", ";
            return output;
        }

        public string getShockerNamesNewLine()
        {
            string output = "";
            foreach (var shocker in Shockers) output += shocker.Name + "\n";
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
        public void startCooldown()
        {
            if (Cooldown <= 0) return; // dont start a cooldown if the trigger doesnt even use them

            CooldownTimer.Stop(); // failsafe for false positives

            if (Duration > 10) CooldownTimer.Interval = Cooldown * 1000 * (int)modifier + 1;
            else CooldownTimer.Interval = Cooldown * 1000 + Duration * 1000 * (int)modifier + 1; // the +1 at the end is a safety net
            CooldownTimer.Start();
        }
        #endregion


    }
}
