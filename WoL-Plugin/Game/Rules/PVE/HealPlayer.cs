using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class HealPlayer : RuleBase
    {
        public override string Name { get; } = "Heal a player";

        public override string Description { get; } = "Triggers whenever you heal a player, intensity scaling strength.";

        public override RuleCategory Category { get; } = RuleCategory.PVE;
        public HealPlayer() { }

        public HealPlayer(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            
        }
    }
}
