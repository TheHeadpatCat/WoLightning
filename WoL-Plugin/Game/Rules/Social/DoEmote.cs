using ImGuiNET;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using System.Numerics;
using WoLightning.WoL_Plugin.Util.UI;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmote : BaseRule
    {
        override public string Name { get; } = "Do a Emote";
        override public string Description { get; } = "Triggers whenever you do a specific Emote.";
        override public RuleCategory Category { get; } = RuleCategory.Social;
        override public bool hasAdvancedOptions { get; } = true;

        List<ushort> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public DoEmote() { }
        public DoEmote(Plugin plugin) : base(plugin) {
        }

        override public void Start()
        {
            if(IsRunning) return;
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteSelf += Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
        }

        override public void Stop()
        {
            if(!IsRunning) return;
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
        }

        public void Check(ushort emoteId)
        {
            if (TriggeringEmotes.Contains(emoteId)) Trigger("You used emote " + emoteId);
        }
        public void Check(IGameObject target, ushort emoteId)
        {
            if (TriggeringEmotes.Contains(emoteId)) Trigger("You used emote " + emoteId);
        }

    }

    [Serializable]
    internal class Option
    {
        public bool usesDefault { get; set; } = true;
        public ShockOptions ShockOptions { get; set; } = new();
        [JsonConstructor] public Option() { }
    }
}
