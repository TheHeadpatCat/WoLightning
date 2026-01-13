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
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class ForgetPartnerBuff : RuleBase
    {
        public override string Name { get; } = "Forget a Partner Buff";

        public override string Description { get; } = "Triggers when you enter combat, without assigning your Partner Buff to someone.";
        public override string Hint { get; } = "The currently supported Buffs are Dance Partner from Dancer and Kardia from Sage.\nYou also need to be in a party with another player.";

        public override RuleCategory Category { get; } = RuleCategory.PVE;

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
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Condition.ConditionChange -= OnConditionChange;
        }

        private void OnConditionChange(ConditionFlag flag, bool value)
        {
            if (flag != ConditionFlag.InCombat || value == false) return;
            if (Service.PartyList.Count < 2) return;
            Check();
        }

        public void Check()
        {
            Player = Service.ObjectTable.LocalPlayer;
            if(Player == null) return;
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
                if (!found) Trigger("You forgot Kardia!");
                return;
            }

            if(JobId == 38) // Dancer
            {
                if (Player.Level < 60) return; // Dancer learns Dance Partner at lvl 60
                bool found = false;
                foreach (var status in Player.StatusList)
                {
                    if (status.StatusId == 1823) { found = true; break; } // Closed Position
                }
                if (!found) Trigger("You forgot Dance Partner!");
                return;
            }
        }

    }
}
