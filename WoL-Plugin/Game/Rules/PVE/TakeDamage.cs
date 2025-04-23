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
    public class TakeDamage : BaseRule
    {
        override public string Name { get; } = "Take Damage";
        override public string Description { get; } = "Triggers whenever you Take Damage for any reason.";
        override public string Hint { get; } = "This will go off ALOT. Literally any damage counts. From mechanics to auto attacks to dots or even fall damage!";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] uint lastHP = 1, lastMaxHP = 1;

        [JsonConstructor]
        public TakeDamage() { }
        public TakeDamage(Plugin plugin) : base(plugin)
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

            if (lastMaxHP != Player.MaxHp)
            {
                lastMaxHP = Player.MaxHp;
                lastHP = lastMaxHP; // avoid false positives from synch and stuff
            }

            if (lastHP > Player.CurrentHp) Trigger("You took damage!");
            lastHP = Player.CurrentHp;
        }

    }
}
