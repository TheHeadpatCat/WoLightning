using Dalamud.Game.ClientState.Objects.Types;
using ImGuiNET;
using System.Collections.Generic;
using System;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using System.Numerics;
using WoLightning.WoL_Plugin.Util.UI;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;
namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class FailMechanic : BaseRule
    {
        override public string Name { get; } = "Fail Mechanic";
        override public string Description { get; } = "Triggers whenever you get a [Vulnerability Up] or [Damage Down]";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] int lastVulnUpStacks = 0, lastDamageDownStacks = 0;

        public FailMechanic(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            IsRunning = true;
            Plugin.Framework.Update += Check;
            Player = Plugin.ClientState.LocalPlayer;
        }

        override public void Stop()
        {
            IsRunning = false;
            Plugin.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            if (Player == null) { Player = Plugin.ClientState.LocalPlayer; return; }

            var Statuses = Player.StatusList;
            if (Statuses == null || Statuses.Length == 0) { lastVulnUpStacks = 0; lastDamageDownStacks = 0; return; }

            bool foundVuln = false, foundDamage = false;
            foreach (var Status in Statuses)
            {
                if (Status == null) continue;
                // Yes. We have to check for the IconId.
                // The StatusId is different for different expansions, while the Name is different through languages.
                if (Status.GameData.Value.Icon >= 17101 && Status.GameData.Value.Icon <= 17116) // Vuln Up
                {
                    var amount = Status.Param;
                    if (amount > lastVulnUpStacks) Trigger("You have failed a Mechanic!");
                    lastVulnUpStacks = amount;
                    foundVuln = true;
                }

                if (Status.GameData.Value.Icon >= 18441 && Status.GameData.Value.Icon <= 18456) // Damage Down
                { 
                    var amount = Status.Param;
                    if (amount > lastDamageDownStacks) Trigger("You have failed a Mechanic!");
                    lastDamageDownStacks = amount;
                    foundDamage = true;
                }

                if (!foundVuln) lastVulnUpStacks = 0;
                if (!foundDamage) lastDamageDownStacks = 0;
            }
        }

    }
}
