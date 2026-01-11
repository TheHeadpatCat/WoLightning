using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util;

namespace WoLightning.WoL_Plugin.Util.Types
{
    public enum CooldownModifier
    {
        Miliseconds = 100,
        Seconds = 1000,
        Minutes = 60000,
        Hours = 3600000,
    }

    [Serializable]
    public class Cooldown : IDisposable
    {
        public int Interval { get; set; } = 1000;
        [JsonIgnore] private TimerPlus Timer { get; set; }
        [JsonIgnore] public bool HasCooldown { get => Timer.TimeLeft > 0; }
        [JsonIgnore] public double TimeLeft { get => Timer.TimeLeft; }

        public Cooldown() {
            Timer = new(Interval,false);
        }

        public Cooldown(int interval)
        {
            Interval = interval;
            Timer = new(Interval,false);
        }

        public void Trigger(bool force = false)
        {
            Timer.Interval = Interval;
            if(!Timer.Enabled || force) Timer.Start();
        }
        public void Trigger(int interval, bool force = false)
        {
            Timer.Interval = interval;
            if (!Timer.Enabled || force) Timer.Start();
        }

        public override string ToString()
        {
            double timeLeft = Timer.TimeLeft;
            int hours = 0, minutes = 0, seconds = 0;
            string result = "";
            if (timeLeft > 3600000)
            {
                hours = (int)timeLeft / 3600000;
                timeLeft -= hours * 3600000;
                result += hours + "h ";
            }

            if(timeLeft > 60000)
            {
                minutes = (int)timeLeft / 60000;
                timeLeft -= minutes * 60000;
                result += minutes + "m ";
            }

            if(timeLeft > 1000)
            {
                seconds = (int)timeLeft / 1000;
                timeLeft -= seconds * 1000;
                result += seconds + "s ";
            }

            result += timeLeft + "ms";

            return result;
        }

        public void Dispose()
        {
            Timer.Dispose();
        }
    }
}
