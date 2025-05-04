using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    public class UseMount : RuleBase
    {
        override public string Name { get; } = "Mount up";
        override public string Description { get; } = "Triggers whenever you use a Mount or ride Pillion.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] bool isMounted = false;
        [JsonIgnore] TimerPlus isMountedTimer = new();
        [JsonIgnore] int SafetyStop = 0;

        [JsonConstructor]
        public UseMount() { }
        public UseMount(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.Condition.ConditionChange += Check;
            isMountedTimer.Elapsed += shockUntilUnmount;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.Condition.ConditionChange -= Check;
            isMountedTimer.Elapsed -= shockUntilUnmount;
        }

        private void Check(ConditionFlag flag, bool value)
        {
            try
            {
                if (flag == ConditionFlag.Mounted) isMounted = value;
                else return;

                if (isMounted && !isMountedTimer.Enabled)
                {
                    isMountedTimer.Interval = ShockOptions.Cooldown + ShockOptions.Duration * 1000 + 3000;
                    isMountedTimer.Start();
                    SafetyStop = 0;
                    Trigger("You got onto a Mount!");
                }
                
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }

        }

        private void shockUntilUnmount(object? sender, ElapsedEventArgs? e)
        {
            if(!isMounted || SafetyStop >= 10)
            {
                isMountedTimer.Stop();
                SafetyStop = 0;
                return;
            }

            Trigger("You are still mounted!");
            SafetyStop += 1;
        }

        }
}
