using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;

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
            if (Player == null) { Player = Plugin.ClientState.LocalPlayer; return; }

            if (Player.IsDead && !IsTriggered) //Player died and Shock has not been triggered yet
            {
                Trigger("You have died!");
                IsTriggered = true;
            }
            if (IsTriggered && !Player.IsDead) //Shock was triggered, and now we are alive again
                IsTriggered = false;
            
        }

    }
}
