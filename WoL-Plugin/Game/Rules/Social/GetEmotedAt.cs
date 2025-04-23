using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class GetEmotedAt : BaseRule
    {
        override public string Name { get; } = "Get Emoted at";
        override public string Description { get; } = "Triggers whenever a specific player sends a specific emote to you.";
        override public string Hint { get; } = "The sending player must not be Blacklisted.\nIf the Whitelist is active, the sending player has to be Whitelisted.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public GetEmotedAt() { }
        public GetEmotedAt(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Plugin.EmoteReaderHooks.OnEmoteIncoming += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.EmoteReaderHooks.OnEmoteIncoming -= Check;
        }

        public override void Draw()
        {
            RuleUI.Draw();
        }

        public void Check(IPlayerCharacter player, ushort emoteId)
        {
            if (!TriggeringEmotes.Contains(emoteId)) return; // Emote isnt listed
            if (player.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return; // The Sender wasnt a player
            Player sendingPlayer = new Player(player.Name.ToString(), (int)player.HomeWorld.RowId);
            Trigger(sendingPlayer + " has used Emote " + emoteId + " on you!", sendingPlayer);
        }

    }
}
