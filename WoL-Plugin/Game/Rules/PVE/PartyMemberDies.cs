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
    public class PartyMemberDies : BaseRule
    {
        override public string Name { get; } = "Partymember dies";
        override public string Description { get; } = "Triggers whenever a partymember dies for whatever reason.";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] bool[] DeadPlayerIndex = [false,false,false,false,false,false,false,false]; //how do i polyfill

        [JsonConstructor]
        public PartyMemberDies() { }
        public PartyMemberDies(Plugin plugin) : base(plugin)
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
                if(Player == null) continue;
                var PlayerObject = Player.GameObject;
                if(PlayerObject == null) continue;

                if ( PlayerObject.IsDead && !DeadPlayerIndex[i]) { DeadPlayerIndex[i] = true; Trigger("A partymember has died!"); }
                if (!PlayerObject.IsDead &&  DeadPlayerIndex[i]) { DeadPlayerIndex[i] = false; }
            }
        }

    }
}
