using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class DontSayWord : BaseRule
    {
        override public string Name { get; } = "Don't say a Enforced Word.";
        override public string Description { get; } = "Triggers whenever you forget to say a word from a list.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        public DontSayWord(Plugin plugin) : base(plugin) { }
    

    }
}
