using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;
namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class FailMechanic : RuleBase
    {
        override public string Name { get; } = "Fail Mechanic";
        override public string Description { get; } = "Triggers whenever you get a [Vulnerability Up] or [Damage Down]";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] ushort lastVulnUpStacks = 0, lastDamageDownStacks = 0;

        [JsonConstructor]
        public FailMechanic() { }
        public FailMechanic(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.Framework.Update += Check;
            Player = Plugin.ClientState.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            try
            {
                Player = Plugin.ClientState.LocalPlayer;
                if (Player == null) { return; }

                var Statuses = Player.StatusList;
                if (Statuses == null || Statuses.Length == 0) { lastVulnUpStacks = 0; lastDamageDownStacks = 0; return; }

                bool foundVuln = false, foundDamage = false;
                foreach (var Status in Statuses)
                {
                    if (Status == null) continue;

                    // Yes. We have to check for the IconId.
                    // The StatusId is different for different expansions, while the Name is different through languages.
                    var icon = Status.GameData.Value.Icon;
                    var amount = Status.Param;

                    if (icon >= 217101 && icon <= 217116) // Vuln Up
                    {
                        if (amount > lastVulnUpStacks) Trigger("You have failed a Mechanic!");
                        lastVulnUpStacks = amount;
                        foundVuln = true;
                        continue;
                    }

                    if (icon >= 218441 && icon <= 218456) // Damage Down
                    {
                        if (amount > lastDamageDownStacks) Trigger("You have failed a Mechanic!");
                        lastDamageDownStacks = amount;
                        foundDamage = true;
                        continue;
                    }

                   
                }
                if (!foundVuln) lastVulnUpStacks = 0;
                if (!foundDamage) lastDamageDownStacks = 0;
            }
            catch (Exception ex)
            {
                Plugin.Error(ex.StackTrace);
            }
        }

    }
}
