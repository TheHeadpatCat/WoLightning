using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    [Serializable]
    public class TakeDamage : BaseRule
    {
        override public string Name { get; } = "Take Damage";
        override public string Description { get; } = "Triggers whenever you Take Damage for any reason.";
        override public string Hint { get; } = "This will go off ALOT.\nLiterally any damage counts.\nFrom mechanics to auto attacks to dots or even fall damage!";
        override public RuleCategory Category { get; } = RuleCategory.PVE;

        [JsonIgnore] IPlayerCharacter Player;
        [JsonIgnore] uint lastHP = 1, lastMaxHP = 1;
        [JsonIgnore] int bufferFrames = 0;

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
            try
            {
                Player = Plugin.ClientState.LocalPlayer;
                if (Player == null) { return; }

                if (bufferFrames > 0)
                {
                    bufferFrames--;
                    return;
                }

                if (lastMaxHP != Player.MaxHp)
                {
                    lastMaxHP = Player.MaxHp;
                    lastHP = lastMaxHP; // avoid false positives from synch and stuff
                    bufferFrames = 180; // give 3 seconds of buffering, for regens and stuff
                }

                if (lastHP > Player.CurrentHp) Trigger("You took damage!");
                lastHP = Player.CurrentHp;
            }
            catch (Exception e) { Plugin.Error(e.StackTrace); }
        }

    }
}
