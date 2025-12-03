using Dalamud.Plugin.Services;
using System;
using System.Linq;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

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
                if (Service.ObjectTable.LocalPlayer == null || Service.PartyList == null || Service.PartyList.Length == 0 || !Service.ObjectTable.LocalPlayer.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat)) { return; }

                int i = -1;
                foreach (var Player in Service.PartyList)
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
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if(e.StackTrace != null) Logger.Error(e.StackTrace); }
        }

    }
}
