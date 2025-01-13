﻿using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmoteTo : BaseRule
    {
        override public string Name { get; } = "Do a Emote to someone";
        override public string Description { get; } = "Triggers whenever you do a specified Emote, while having a specified player targeted.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public Dictionary<ushort,List<Player>> TriggeringEmotes { get; set; } = new();
        
        public DoEmoteTo(Plugin plugin) : base(plugin) { }

        override public void Start()
        {
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
        }

        override public void Stop() 
        {
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
        }

        public void Check(IGameObject target,ushort emoteId)
        {
            List<Player>? players = TriggeringEmotes[emoteId];
            if (players == null) return; // Emote isnt listed
            if (target.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Player) return; // We arent targeting a player.
            IPlayerCharacter character = (IPlayerCharacter)target;
            Player targetedPlayer = new Player(character.Name.ToString(), (int)character.HomeWorld.RowId);
            if(players.Contains(targetedPlayer)) Trigger("You used emote " + emoteId + " on " + targetedPlayer);
        }
    }
}
