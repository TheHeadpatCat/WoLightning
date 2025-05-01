using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class PartyWipes : RuleBase
    {
        override public string Name { get; } = "Party wipe";
        override public string Description { get; } = "Triggers whenever the entire Party dies.";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] bool[] DeadPlayerIndex = [false, false, false, false, false, false, false, false]; //how do i polyfill
        [JsonIgnore] bool isTriggered = false;

        [JsonConstructor]
        public PartyWipes() { }
        public PartyWipes(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.Framework.Update += Check;
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
                if (Plugin.ClientState.LocalPlayer == null || Plugin.PartyList == null || Plugin.PartyList.Length == 0 || !Plugin.ClientState.LocalPlayer.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat)) { return; }

                int i = -1;
                foreach (var Player in Plugin.PartyList)
                {
                    i++;
                    if (Player == null) continue;
                    var PlayerObject = Player.GameObject;
                    if (PlayerObject == null) continue;

                    if (PlayerObject.IsDead && !DeadPlayerIndex[i]) { DeadPlayerIndex[i] = true; } //player died
                    if (!PlayerObject.IsDead && DeadPlayerIndex[i]) { DeadPlayerIndex[i] = false; isTriggered = false; } //player got revived - somehow
                    if (!isTriggered && DeadPlayerIndex.All((x) => x == true)) { Trigger("The party is wiped!"); isTriggered = true; } //everyone is dead and we havent triggered the shock
                }
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }
        }

    }
}
