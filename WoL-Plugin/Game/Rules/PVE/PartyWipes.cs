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
using System.Linq;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class PartyWipes : BaseRule
    {
        override public string Name { get; } = "Party wipe";
        override public string Description { get; } = "Triggers whenever ´the entire Party dies.";
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
            IsRunning = true;
            Plugin.Framework.Update += Check;
        }

        override public void Stop()
        {
            IsRunning = false;
            Plugin.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            if (Plugin.ClientState.LocalPlayer == null || Plugin.PartyList == null || Plugin.PartyList.Length == 0) { return; }

            int i = -1;
            foreach (var Player in Plugin.PartyList)
            {
                i++;
                if (Player == null) continue;
                var PlayerObject = Player.GameObject;
                if (PlayerObject == null) continue;

                if (PlayerObject.IsDead && !DeadPlayerIndex[i]) { DeadPlayerIndex[i] = true;  } //player died
                if (!PlayerObject.IsDead && DeadPlayerIndex[i]) { DeadPlayerIndex[i] = false; isTriggered = false; } //player got revived - somehow
                if (!isTriggered && DeadPlayerIndex.All((x) => x == true)) { Trigger("The party is wiped!"); isTriggered = true; } //everyone is dead and we havent triggered the shock
            }
        }

    }
}
