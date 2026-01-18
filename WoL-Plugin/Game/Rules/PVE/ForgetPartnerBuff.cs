using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Statuses;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class ForgetPartnerBuff : RuleBase
    {
        public override string Name { get; } = "Forget a Partner Buff";

        public override string Description { get; } = "Triggers when you enter combat, without assigning your Partner Buff to someone.";
        public override string Hint { get; } = "The currently supported Buffs are Dance Partner from Dancer and Kardia from Sage.\nYou also need to be in a party with another player and in a Duty.";
        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public override bool hasExtraButton { get; } = true;

        public bool IsRepeating { get; set; } = false;
        [JsonIgnore] TimerPlus RepeatTimer = new();

        [JsonIgnore] IPlayerCharacter Player;
        public ForgetPartnerBuff() { }
        public ForgetPartnerBuff(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Condition.ConditionChange += OnConditionChange;
            RepeatTimer.Interval = ShockOptions.getDurationOpenShock() + 5000;
            RepeatTimer.AutoReset = false;
            RepeatTimer.Elapsed += OnRepeatTimerElapsed;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Condition.ConditionChange -= OnConditionChange;
            if (RepeatTimer == null) return;
            RepeatTimer.Elapsed -= OnRepeatTimerElapsed;
            RepeatTimer.Stop();
            RepeatTimer.Dispose();
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.InCombat) return;
            if (value == false) RepeatTimer.Stop();
            if (Service.PartyList.Count < 2) return;
            Check();
        }

        public void Check()
        {
            Player = Service.ObjectTable.LocalPlayer;
            if(Player == null) return;

            if (!Service.Condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95)) return; // we arent actually in any content right now

            uint JobId = Player.ClassJob.RowId;
            Logger.Log(4,"JobId: " + JobId);
            if (JobId != 40 && JobId != 38) return; // neither sage nor dancer

            if(JobId == 40) // Sage
            {
                if (Player.Level < 4) return; // Sage learns Kardia at lvl 4
                bool found = false;
                foreach (var status in Player.StatusList)
                {
                    if (status.StatusId == 2604) { found = true; break; }
                }
                if (!found) { 
                    Trigger("You forgot Kardia!"); 
                    if(IsRepeating) RepeatTimer.Start(); 
                    return; 
                }
            }

            if(JobId == 38) // Dancer
            {
                if (Player.Level < 60) return; // Dancer learns Dance Partner at lvl 60
                bool found = false;
                foreach (var status in Player.StatusList)
                {
                    if (status.StatusId == 1823) { found = true; break; } // Closed Position
                }
                if (!found) { 
                    Trigger("You forgot Dance Partner!");
                    if (IsRepeating) RepeatTimer.Start(); 
                    return; 
                }
                return;
            }
        }

        private void OnRepeatTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Check();
        }

        public override void DrawExtraButton()
        {
            bool repeat = IsRepeating;
            if (ImGui.Checkbox("Keep Triggering until applied?##repeatPartnerBuff", ref repeat))
            {
                IsRepeating = repeat;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
        }
    }
}
