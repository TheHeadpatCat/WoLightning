using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmoteTo : BaseRule
    {
        override public string Name { get; } = "Do a Emote to someone";
        override public string Description { get; } = "Triggers whenever you do a specific Emote, while having a player targeted.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public Dictionary<ushort, List<Player>> TriggeringEmotes { get; set; } = new();

        [JsonConstructor]
        public DoEmoteTo() { }
        public DoEmoteTo(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
            IsRunning = true;
        }

        override public void Stop()
        {
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
            IsRunning = false;
        }

        public void Check(IGameObject target, ushort emoteId)
        {
            List<Player>? players = TriggeringEmotes[emoteId];
            if (players == null) return; // Emote isnt listed
            if (target.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return; // We arent targeting a player.
            IPlayerCharacter character = (IPlayerCharacter)target;
            Player targetedPlayer = new Player(character.Name.ToString(), (int)character.HomeWorld.RowId);
            if (players.Contains(targetedPlayer)) Trigger("You used emote " + emoteId + " on " + targetedPlayer);
        }
    }
}
