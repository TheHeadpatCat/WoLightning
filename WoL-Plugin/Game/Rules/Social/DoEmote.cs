using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoLightning.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    [Serializable]
    public class DoEmote : BaseRule
    {
        new public string Name { get; } = "Do a Emote";
        new public string Description { get; } = "Triggers whenever you do a specified Emote";
        new public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();
        
        public DoEmote(Plugin plugin, ShockOptions shockOptions)
        {
            Plugin = plugin;
            ShockOptions = shockOptions;
        }

        new public void Start()
        {
            Plugin.EmoteReaderHooks.OnEmoteSelf += Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing += Check;
        }

        new public void Stop() 
        {
            Plugin.EmoteReaderHooks.OnEmoteSelf -= Check;
            Plugin.EmoteReaderHooks.OnEmoteOutgoing -= Check;
        }

        public void Check(ushort emoteId)
        {
            if(TriggeringEmotes.Contains(emoteId))Trigger("You used emote " + emoteId);
        }
        public void Check(IGameObject target,ushort emoteId)
        {
            if (TriggeringEmotes.Contains(emoteId)) Trigger("You used emote " + emoteId);
        }

        new public void Trigger(string Text)
        {
            if (ShockOptions.hasCooldown()) return;
            Triggered?.Invoke(this);
            Plugin.sendNotif(Text);
            ShockOptions.startCooldown();
        }
    }
}
