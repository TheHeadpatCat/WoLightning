using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Collections.Generic;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class GetEmotedAt : BaseRule
    {
        override public string Name { get; } = "Get emoted at";
        override public string Description { get; } = "Triggers whenever a specified player sends a emote to you.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public Dictionary<ushort, SpecificPlayer> TriggeringEmotes { get; set; } = new();

        public GetEmotedAt(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += Check;
        }

        override public void Stop()
        {
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= Check;
        }

        public override void Draw()
        {
            RuleUI.Draw();
        }

        public void Check(IPlayerCharacter player, ushort emoteId)
        {
            SpecificPlayer? targets = TriggeringEmotes[emoteId];
            if (targets == null) return;
            if (targets.Compare(new Player(player))) Trigger("You used Emote " + emoteId + " on a Player!");
        }

    }
}
