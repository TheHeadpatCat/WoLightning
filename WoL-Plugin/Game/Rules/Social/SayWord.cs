using System.Collections.Generic;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util.Types;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class SayWord : BaseRule
    {
        public override string Name { get; } = "Say a Banned Word";

        public override string Description { get; } = "Triggers whenever you say a word from a list.";

        public override RuleCategory Category { get; } = RuleCategory.Social;

        public Dictionary<SpecificWord, ShockOptions> BannedWords { get; set; }

        public SayWord(Plugin plugin) : base(plugin) { }


    }
}
