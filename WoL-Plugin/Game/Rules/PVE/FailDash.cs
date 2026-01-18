using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using WoLightning.Util;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class FailDash : RuleBase
    {
        public override string Name { get; } = "Fail a Dash";
        public override string Description { get; } = "Triggers whenever you use a Dash and die within 3 seconds of it.";
        public override string Hint { get; } = "";
        public override RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] TimerPlus Timer = new();

        public FailDash() { }
        public FailDash(Plugin plugin) : base(plugin){ }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.ActionReaderHooks.ActionUsed += Check;
            Player = Service.ObjectTable.LocalPlayer;
            Timer.Interval = 3000;
            Timer.AutoReset = false;
            Timer.Elapsed += OnTimerElapsed;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.ActionReaderHooks.ActionUsed -= Check;
            Timer.Stop();
            Timer.Elapsed -= OnTimerElapsed;
        }

        private void Check(Lumina.Excel.Sheets.Action action)
        {
            if(Player == null) { 
                Player = Service.ObjectTable.LocalPlayer; 
                if(Player == null) return; 
            }

            if (IsDash(action.RowId)) Timer.Start();

        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (Player.IsDead) //Player died and Shock has not been triggered yet
            {
                Trigger("You failed a Dash!");
            }
        }

        private bool IsDash(uint id)
        {
            return id == 16461 // PLD Intervene
                || id == 7386 || id == 25753 // WAR Onslaught, Primal Rend
                || id == 36926 // DRK Shadowstride
                || id == 36934 // GNB Trajectory
                || id == 37008 // WHM Aetherial Shift
                || id == 24295 // SGE Icarus
                || id == 25762 // MNK Thunderclap
                || id == 36951 || id == 96 || id == 16480 || id == 94 // DRG Winged Glide, Dragonfire Dive, Stardiver, Elusive Jump
                || id == 2262 || id == 25777 // NIN Shukuchi, Forked Raiju
                || id == 7492 || id == 7493 // SAM Hissatsu: Gyoten, Hissatsu, Yaten
                || id == 24401 || id == 24402 || id == 24403 // RPR Hell's Ingress, Hell's Egress, Egress
                || id == 34646 // VPR Slither
                || id == 112 // BRD Repelling Shot
                || id == 16010 // DNC EEn Avant
                || id == 0 || id == 7419 // BLM Atherial Manipulation, Between the Lines
                || id == 25835 // SMN Crimson Cyclone
                || id == 7506 || id == 7515 // RDM Crops-a-corps, Displacement
                || id == 34684; // PCT Smudge
        }
    }
}
