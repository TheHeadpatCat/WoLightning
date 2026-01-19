using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class ForgetTankstance : RuleBase
    {
        public override string Name { get; } = "Forget Tankstance";

        public override string Description { get; } = "Triggers whenever you forget to activate your Tankstance.";
        public override string Hint { get; } = "This Rule will try to respect general Offtank Rules.\nIf another Tank has their stance on, you don't need it.";

        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public override bool hasExtraButton { get; } = true;

        public bool TriggerOnDoubleStance { get; set; } = false;

        [JsonIgnore] IPlayerCharacter Player;

        public ForgetTankstance() { }
        public ForgetTankstance(Plugin plugin) : base(plugin)
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
            if (flag == ConditionFlag.InCombat && value == true)
            {
                CheckStance();
                return;
            }

        }

        private void CheckStance()
        {
            Player = Service.ObjectTable.LocalPlayer;
            if(Player == null) return;

            if (!Service.Condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95)) return; // we arent actually in any content right now

            if (!IsJobATank(Player.ClassJob.Value)) return;

            if(Service.PartyList.Count <= 0) return;

            bool foundStance = false;
            foreach (var status in Player.StatusList)
            {
                if (IsStatusAStance(status.StatusId))
                {
                    Logger.Log(4, "Found Tankstance");
                    foundStance= true; 
                    break;
                }
            }

            if (foundStance && !TriggerOnDoubleStance) return;

            int tankCount = 1;
            int enemyCount = 0; // todo: add enemy counter
            bool otherTankHasStance = false;

            var NearbyPlayers = Service.ObjectTable.PlayerObjects;

            foreach (var nearbyPlayer in NearbyPlayers)
            {
                if(Player.GameObjectId == nearbyPlayer.GameObjectId) continue;
                if(IsJobATank(nearbyPlayer.ClassJob.Value))
                {
                    tankCount++;
                    if (otherTankHasStance) continue;
                    foreach (var status in nearbyPlayer.StatusList)
                    {
                        if (IsStatusAStance(status.StatusId))
                        {
                            Logger.Log(4, $"Found Stance on {nearbyPlayer.Name.TextValue}");
                            otherTankHasStance = true;
                            break;
                        }
                    }
                }
            }

            if (!otherTankHasStance && !foundStance)
            {
                Trigger("You forgot your Tank Stance!");
                return;
            }

            if (otherTankHasStance && foundStance)
            {
                // todo: do check if there are multiple enemies here;
                if (TriggerOnDoubleStance) Trigger("Someone else has their Tankstance on!");
                return;
            }

        }

        // todo: move to helper class
        private bool IsJobATank(ClassJob job)
        {
            return job.RowId == 19 || job.RowId == 21 || job.RowId == 32 || job.RowId == 37;
        } 

        private bool IsStatusAStance(uint id)
        {
            return id == 79  || id == 393 || id == 2843  // iron will from PLD
                || id == 91  || id == 3124               // defiance from WAR
                || id == 743 || id == 1397               // grit from DRK
                || id == 392 || id == 1833;              // royal guard from GNB
        }


        public override void DrawExtraButton()
        {
            bool doubleStance = TriggerOnDoubleStance;
            if(ImGui.Checkbox("Trigger if another Tank also has Stance on?",ref doubleStance))
            {
                TriggerOnDoubleStance = doubleStance;
                Plugin.Configuration.SaveCurrentPreset();
            }
        }

        }
    }
