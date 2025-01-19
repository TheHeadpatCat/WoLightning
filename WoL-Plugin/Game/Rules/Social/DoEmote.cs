using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmote : BaseRule
    {
        override public string Name { get; } = "Do a Emote";
        override public string Description { get; } = "Triggers whenever you do a specified Emote";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        public DoEmote(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteSelf += Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
        }

        override public void Stop()
        {
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteSelf -= Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
        }

        public override void Draw()
        {
            RuleUI.Draw();
        }

        public void Check(ushort emoteId)
        {
            //if(TriggeringEmotes.Contains(emoteId))Trigger("You used emote " + emoteId);
            Trigger("You used emote " + emoteId);
        }
        public void Check(IGameObject target, ushort emoteId)
        {
            if (TriggeringEmotes.Contains(emoteId)) Trigger("You used emote " + emoteId);

        }
    }
}
