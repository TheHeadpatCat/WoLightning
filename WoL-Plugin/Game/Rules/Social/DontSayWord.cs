using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class DontSayWord : BaseRule
    {
        override public string Name { get; } = "Don't say a Enforced Word.";
        override public string Description { get; } = "Triggers whenever you forget to say a word from a list.";
        override public string Hint { get; } = "This will go off whenever NONE of the words were said.\nAs long as you say atleast one of them, you are safe!";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        public DontSayWord(Plugin plugin) : base(plugin) { }
    

    }
}
