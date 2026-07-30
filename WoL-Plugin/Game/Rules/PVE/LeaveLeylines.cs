using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class LeaveLeylines : RuleBase
    {
        public override string Name { get; } = "Leave Leylines";

        public override string Description { get; } = "Triggers whenever you leave Blackmage Leylines for too long";

        public override RuleCategory Category { get; } = RuleCategory.PVE;

        public override bool hasExtraButton { get; } = true;

        public bool IsOnlyInCombat { get; set; } = false;

        [JsonIgnore] public TimerPlus RepeatTimer = new();

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] int UpdateInterval = 0;


        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Framework.Update += OnUpdate;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Framework.Update -= OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            if(UpdateInterval > 0)
            {
                UpdateInterval--;
                return;
            }
            UpdateInterval = 60;

            Check();
        }

        public void Check()
        {
            if (Player == null) Player = Service.ObjectTable.LocalPlayer;

            if (!Service.Condition.Any(ConditionFlag.InCombat) && IsOnlyInCombat)
            {
                RepeatTimer.Stop();
                return;
            }

            uint JobId = Player.ClassJob.RowId;
            if (JobId != 25) return; 
            if (Player.Level < 52) return; // Blackmage learns Leylines at 52
            bool foundLeylines = false, foundCircleOfPower = false;
            foreach (var status in Player.StatusList)
            {
               if (status.StatusId == 737) { foundLeylines = true; continue; } 
               if (status.StatusId == 738) { foundCircleOfPower = true; continue; }
            }
            if (foundLeylines && !foundCircleOfPower)
            {
               Trigger("You stepped out of Leylines!");
                UpdateInterval = ShockOptions.getDurationOpenShock() / 1000 * 60 + 240;
               return;
            }
        }

        private void OnRepeatTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Check();
        }

        public override void DrawExtraButton()
        {
            bool inCombat = IsOnlyInCombat;
            if (ImGui.Checkbox("Trigger only in Combat?##LeylinesOnlyInCombat", ref inCombat))
            {
                IsOnlyInCombat = inCombat;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
        }
    }
}
