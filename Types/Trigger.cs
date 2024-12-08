﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WoLightning.Types
{
    public enum OpMode
    {
        Shock = 0,
        Vibrate = 1,
        Beep = 2
    }

    [Serializable]
    public class Trigger
    {

        public string Name { get; set; } // Name of the Trigger itself, used for Logging
        public OpMode OpMode { get; set; } = OpMode.Shock;
        public int Intensity { get; set; } = 1;
        public int Duration { get; set; } = 1;
        public int Cooldown { get; set; } = 0;

        public List<Shocker> Shockers { get; set; } = new(); // List of all Shocker Codes to run on
        public bool Randomize { get; set; } = false;
        public int RandomizeAmount { get; set; } = 1;

        public bool hasCustomData { get; set; } = false;
        public Dictionary<String, int[]>? CustomData { get; set; } // Data that gets generated by the User

        public string NotifMessage { get; set; }

        [NonSerialized] public bool isModalOpen = true; // Used for Configwindow
        [NonSerialized] public bool isOptionsOpen = false;
        [NonSerialized] public TimerPlus CooldownTimer = new();
        [NonSerialized] public bool hasBeenReset = false;

        


        public Trigger(string Name, string NotifMessage, bool hasCustomData)
        {
            this.Name = Name;
            this.NotifMessage = NotifMessage;

            this.hasCustomData = hasCustomData;
            if (hasCustomData) setupCustomData();

            CooldownTimer.AutoReset = false;
            CooldownTimer.Stop();
        }

        public bool Validate()
        {
            if (NotifMessage == null) return false;
            if ((int)OpMode < 0 || (int)OpMode > 2) return false;
            if (Intensity < 1) Intensity = 1;
            if (Intensity > 100) Intensity = 100;
            if (Duration < 1) Duration = 1;
            if (Duration > 10 && Duration != 100 && Duration != 300) Duration = 10;
            return !(Shockers.Count < 1 || Shockers.Count > 5);
        }

        public bool ValidateNoShockers()
        {
            if (NotifMessage == null) return false;
            if ((int)OpMode < 0 || (int)OpMode > 2) return false;
            if (Intensity < 1) Intensity = 1;
            if (Intensity > 100) Intensity = 100;
            if (Duration < 1) Duration = 1;
            if (Duration > 10 && Duration != 100 && Duration != 300) Duration = 10;
            return true;
        }
        public bool IsEnabled()
        {
            return Shockers.Count > 0;
        }

        public override string ToString()
        {
            return $"[Trigger] Name:{Name} Mode:{OpMode} {Intensity}%|{Duration}s Cooldown:{Cooldown}s Applied to {Shockers.Count} Shockers.";
        }

        public string durationString()
        {
            string output = "";
            switch(Duration) { case 100: output = "0.1s"; break; case 300: output = "0.3s"; break; default: output = Duration + "s"; break; }
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

        public void setupCustomData()
        {
            if (CustomData == null) CustomData = new Dictionary<String, int[]>();
            if (CustomData.Count > 0) return;
            switch (this.Name)
            {
                case "FailMechanic":
                    CustomData.Add("Proportional", [0, 1]);
                    break;
                case "TakeDamage":
                    CustomData.Add("Proportional", [0, 0]);
                    break;
                case "SayBadWord":
                    CustomData.EnsureCapacity(1);
                    break;
                case "DontSayWord":
                    CustomData.EnsureCapacity(1);
                    break;
            }
        }

        public Trigger Clone()
        {
            return (MemberwiseClone() as Trigger)!;
        }

        public bool hasCooldown()
        {
            return CooldownTimer.TimeLeft > 0;
        }

        public void startCooldown()
        {
            if (Cooldown <= 0) return; // dont start a cooldown if the trigger doesnt even use them
            
            CooldownTimer.Stop(); // failsafe for false positives

            if (Duration > 10) CooldownTimer.Interval = Cooldown * 1000 + 1;
            else CooldownTimer.Interval = Cooldown * 1000 + Duration * 1000 + 1; // the +1 at the end is a safety net
            CooldownTimer.Start();
        }

    }

    [Serializable]
    public class RegexTrigger : Trigger
    {
        public Guid GUID = Guid.NewGuid();
        public string RegexString = "(?!)";
        public Regex? Regex = new Regex("(?!)");
        public RegexTrigger(string Name,bool hasCustomData) : base(Name,"Custom Trigger",hasCustomData)
        {
            base.Name = Name;
        }
    }
}
