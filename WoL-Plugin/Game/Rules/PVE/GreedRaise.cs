using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class GreedRaise : RuleBase
    {
        public override string Name { get; } = "Greed on a Resurrection";

        public override string Description { get; } = "Triggers whenever you cast a raise on someone and then have another Partymember die.";

        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public override bool hasExtraButton { get; } = true;

        public int DangerPeriod { get; set; } = 5;
        public bool IncludeDPS { get; set; } = false;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] bool[] PartyAliveIndex = [false, false, false, false, false, false, false, false];
        [JsonIgnore] double MonitoringMs = 0;
        [JsonIgnore] double MonitoringDelay = 0;

        public GreedRaise() { }
        public GreedRaise(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.ActionReaderHooks.ActionUsed += CheckActionUsed;
            Player = Service.ObjectTable.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.ActionReaderHooks.ActionUsed -= CheckActionUsed;
            Service.Framework.Update -= MonitorPartyDeath;
        }

        private void CheckActionUsed(Lumina.Excel.Sheets.Action action)
        {
            if (Player == null)
            {
                Player = Service.ObjectTable.LocalPlayer;
                if (Player == null) return;
            }

            if (Service.PartyList.Count < 2) return;

            if (Player.ClassJob.RowId != 24 // whm
               && Player.ClassJob.RowId != 28 // sch
               && Player.ClassJob.RowId != 33 // ast
               && Player.ClassJob.RowId != 40 // sge
               && Player.ClassJob.RowId != 27 // smn
               && Player.ClassJob.RowId != 35)// rdm
                return; // We are not a raiser

            if (!IncludeDPS && (Player.ClassJob.RowId == 27 || Player.ClassJob.RowId == 35)) return; // we are a dps, but the option is disabled.

            if (IsRaise(action.RowId))
            {
                if (MonitoringMs > 0) return;
                Logger.Log(4, "Raise found, monitoring...");

                foreach (var (i,member) in Service.PartyList.Index())
                {
                    if(member == null) continue;
                    PartyAliveIndex[i] = member.CurrentHP > 0;
                    Logger.Log(4, "Member at " + i + " is " + PartyAliveIndex[i]);
                }

                MonitoringMs = DangerPeriod * 1000;
                Service.Framework.Update += MonitorPartyDeath;
            }

        }

        private void MonitorPartyDeath(IFramework framework)
        {
            if (Player == null)
            {
                Player = Service.ObjectTable.LocalPlayer;
                if (Player == null) return;
            }
            if (Service.PartyList.Count < 2) return;

            if (MonitoringMs <= 0)
            {
                Service.Framework.Update -= MonitorPartyDeath;
                Logger.Log(4, "Raise Monitoring completed.");
                return;
            }
            MonitoringMs -= framework.UpdateDelta.TotalMilliseconds;

            if(MonitoringDelay > 0)
            {
                MonitoringDelay -= framework.UpdateDelta.TotalMilliseconds;
                return;
            }
            MonitoringDelay = 300;

            foreach (var (i, member) in Service.PartyList.Index())
            {
                if (member == null) continue;
                if(PartyAliveIndex[i] && member.CurrentHP == 0)
                {
                    Logger.Log(4, "Member at " + i + " has died.");
                    MonitoringMs = 0;
                    Trigger("Someone died because of your raise!");
                    return;
                }
            }
        }

        private bool IsRaise(uint id)
        {
            return id == 173 // Resurrection
                || id == 125 // Raise
                || id == 3603 // Ascend
                || id == 24287 // Egeiro
                || id == 7523; // Verraise
        }

        public override void DrawExtraButton()
        {
            ImGui.Separator();
            int danger = DangerPeriod;
            ImGui.SetNextItemWidth(250);
            if (ImGui.SliderInt("Danger period (s)##DangerPeriodGreedRaise", ref danger, 2, 30))
            {
                DangerPeriod = danger;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            bool dps = IncludeDPS;
            if (ImGui.Checkbox("Include SMN & RDM?##IncludeDPSGreedRaise", ref dps))
            {
                IncludeDPS = dps;
                Plugin.Configuration.SaveCurrentPresetScheduled();
            }
            ImGui.Separator();
        }
    }
}
