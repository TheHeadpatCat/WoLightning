using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class PartyMemberDies : RuleBase
    {
        override public string Name { get; } = "Partymember dies";
        override public string Description { get; } = "Triggers whenever a partymember dies for whatever reason.";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] bool[] DeadPlayerIndex = [false, false, false, false, false, false, false, false]; //how do i polyfill
        [JsonIgnore] int LastPartySize = 0;

        [JsonConstructor]
        public PartyMemberDies() { }
        public PartyMemberDies(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.Framework.Update += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.Framework.Update -= Check;
        }

        private void Check(IFramework framework)
        {
            try
            {
                if (Service.ObjectTable.LocalPlayer == null || Service.PartyList == null || Service.PartyList.Length == 0) { return; }

                if (LastPartySize != Service.PartyList.Length) // If someone leaves or enters the party, reset the index.
                {
                    DeadPlayerIndex = [false, false, false, false, false, false, false, false]; //how do i polyfill
                    LastPartySize = Service.PartyList.Length;
                }


                int i = -1;
                foreach (var Player in Service.PartyList)
                {
                    i++;
                    if (Player == null) continue;
                    var PlayerObject = Player.GameObject;
                    if (PlayerObject == null) continue;

                    if (PlayerObject.IsDead && !DeadPlayerIndex[i]) { DeadPlayerIndex[i] = true; Trigger("A partymember has died!"); }
                    if (!PlayerObject.IsDead && DeadPlayerIndex[i]) { DeadPlayerIndex[i] = false; }
                }
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

    }
}
