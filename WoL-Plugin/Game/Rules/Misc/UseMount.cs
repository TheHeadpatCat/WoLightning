using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using System;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    public class UseMount : RuleBase
    {
        override public string Name { get; } = "Mount up";
        override public string Description { get; } = "Triggers whenever you use a Mount or ride Pillion.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        override public bool hasExtraButton { get; } = true;

        public bool IncludePillion { get; set; } = true;

        [JsonIgnore] bool isMounted = false;
        [JsonIgnore] bool isMountedPillion = false;
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
            Service.Condition.ConditionChange += Check;
            isMountedTimer.Elapsed += shockUntilUnmount;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Condition.ConditionChange -= Check;
            isMountedTimer.Elapsed -= shockUntilUnmount;
        }

        private void Check(ConditionFlag flag, bool value)
        {
            try
            {
                //Logger.Log(3, flag.ToString());
                if (flag == ConditionFlag.Mounted) isMounted = value;
                else if (flag == ConditionFlag.RidingPillion) isMountedPillion = value;
                else return;


                if ((isMounted || (IncludePillion && isMountedPillion)) && !isMountedTimer.Enabled)
                {
                    isMountedTimer.Interval = ShockOptions.Cooldown + ShockOptions.Duration * 1000 + 3000;
                    isMountedTimer.Start();
                    SafetyStop = 0;
                    Trigger("You got onto a Mount!");
                }

            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }

        }

        private void shockUntilUnmount(object? sender, ElapsedEventArgs? e)
        {
            if ((!isMounted && (!isMountedPillion && IncludePillion)) || SafetyStop >= 10)
            {
                isMountedTimer.Stop();
                SafetyStop = 0;
                return;
            }

            Trigger("You are still mounted!");
            SafetyStop += 1;
        }

        public override void DrawExtraButton()
        {
            ImGui.SameLine();
            bool includePillion = IncludePillion;
            if (ImGui.Checkbox("Include Pillion?", ref includePillion))
            {
                IncludePillion = includePillion;
                Plugin.Configuration.saveCurrentPreset();
            }
        }

    }
}
