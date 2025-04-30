using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class PartyMemberDies : RuleBase
    {
        override public string Name { get; } = "Partymember dies";
        override public string Description { get; } = "Triggers whenever a partymember dies for whatever reason.";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] bool[] DeadPlayerIndex = [false, false, false, false, false, false, false, false]; //how do i polyfill

        [JsonConstructor]
        public PartyMemberDies() { }
        public PartyMemberDies(Plugin plugin) : base(plugin)
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
                if (Plugin.ClientState.LocalPlayer == null || Plugin.PartyList == null || Plugin.PartyList.Length == 0) { return; }

                int i = -1;
                foreach (var Player in Plugin.PartyList)
                {
                    i++;
                    if (Player == null) continue;
                    var PlayerObject = Player.GameObject;
                    if (PlayerObject == null) continue;

                    if (PlayerObject.IsDead && !DeadPlayerIndex[i]) { DeadPlayerIndex[i] = true; Trigger("A partymember has died!"); }
                    if (!PlayerObject.IsDead && DeadPlayerIndex[i]) { DeadPlayerIndex[i] = false; }
                }
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }
}

    }
    } 
