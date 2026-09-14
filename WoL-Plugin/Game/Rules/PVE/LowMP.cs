using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;
using WoLightning.WoL_Plugin.Util.UI_Elements;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class LowMP : RuleBase
    {
        public override string Name { get; } = "Have low MP";

        public override string Description { get; } = "Triggers repeatedly when you have low MP";
        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public override bool hasExtraButton { get; } = true;

        public int TriggerThreshold { get; set; } = 70;
        public int MaximumStrengthThreshold { get; set; } = 20;
        public int CheckInterval { get; set; } = 5;

        public bool ApplyToDuration { get; set; } = true;
        public bool ApplyToIntensity { get; set; } = true;


        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] double CheckMs = 0;
        float trigger;
        float max;

        public LowMP() { }
        public LowMP(Plugin plugin) : base(plugin) { }
        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            UpdateThresholds();
            Service.Framework.Update += Check;
            Player = Service.ObjectTable.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            if(Player == null)
            {
                Player = Service.ObjectTable.LocalPlayer;
                return;
            }

            if(CheckMs > 0)
            {
                CheckMs -= framework.UpdateDelta.TotalMilliseconds;
                return;
            }

            CheckMs = CheckInterval * 1000;

            if (Player.CurrentMp >= (TriggerThreshold * 100))
                return;
            

            float currentMPpercent = (float)Player.CurrentMp / Player.MaxMp;
            float result = MathF.Min(1 - (currentMPpercent - max) / (trigger - max), 1);

            Logger.Log(4, "Current: " + currentMPpercent * 100 + "% - " + "trigger/min: " + TriggerThreshold + "%" + MaximumStrengthThreshold + "% - result: " + result * 100 + "%");

            if (ApplyToDuration || ApplyToIntensity)
            {
                ShockOptions newOptions = new ShockOptions(ShockOptions);
                if(ApplyToIntensity) 
                    newOptions.Intensity = (int)(newOptions.Intensity * result);
                if(ApplyToDuration)
                    newOptions.Duration = (int)(newOptions.Duration * result);

                CheckMs = CheckInterval * 1000 + newOptions.getDurationOpenShock();

                Trigger("Your MP is too low!", null, newOptions);
                return;
            }

            CheckMs = CheckInterval * 1000 + ShockOptions.getDurationOpenShock();

            Trigger("Your MP is too low!");
        }

        public override void DrawExtraButton()
        {
            ImGui.Separator();
            int triggerMp = TriggerThreshold;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Trigger at/below this MP%##LowMPTriggerMP", ref triggerMp, 0, 100))
            {
                TriggerThreshold = triggerMp;
                Plugin.Configuration.SaveCurrentPresetScheduled();
                UpdateThresholds();
            }
            int maxThreshold = MaximumStrengthThreshold;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("100% Strength at this MP%##LowMPMaxThreshold", ref maxThreshold, 0, 100))
            {
                MaximumStrengthThreshold = maxThreshold;
                Plugin.Configuration.SaveCurrentPresetScheduled();
                UpdateThresholds();
            }
            HoverText.ShowSameLine(" (?)", "Once MP is below this %, the rule will use 100% of the settings below.");

            ImGui.Text("Apply proportional % to:");
            bool applyDuration = ApplyToDuration;
            if(ImGui.Checkbox("Duration", ref applyDuration))
            {
                ApplyToDuration = applyDuration;
                Plugin.Configuration.SaveCurrentPreset();
            }
            ImGui.SameLine();
            bool applyIntensity = ApplyToIntensity;
            if (ImGui.Checkbox("Intensity", ref applyIntensity))
            {
                ApplyToIntensity = applyIntensity;
                Plugin.Configuration.SaveCurrentPreset();
            }

            int timer = CheckInterval;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Check Interval (s)##LowMPCheckInterval", ref timer, 2, 30))
            {
                CheckInterval = timer;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            ImGui.Separator();
        }

        private void UpdateThresholds()
        {
            trigger = TriggerThreshold / 100f;
            max = MaximumStrengthThreshold / 100f;
        }

    }
}
