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
    public class Die : BaseRule
    {
        override public string Name { get; } = "Die";
        override public string Description { get; } = "Triggers whenever you Die for whatever reason.";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] bool IsTriggered = false;

        [JsonConstructor]
        public Die() { }
        public Die(Plugin plugin) : base(plugin)
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
            if(Player == null) { Player = Plugin.ClientState.LocalPlayer; return; }
            
            if(Player.IsDead && !IsTriggered) //Player died and Shock has not been triggered yet
            {
                Trigger("You have died!");
                IsTriggered = true;
            }
            if (IsTriggered && !Player.IsDead) //Shock was triggered, and now we are alive again
                IsTriggered = false; 
        }

    }
}
