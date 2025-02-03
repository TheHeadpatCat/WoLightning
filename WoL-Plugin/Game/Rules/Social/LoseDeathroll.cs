using System.Collections.Generic;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class LoseDeathroll : BaseRule
    {
        override public string Name { get; } = "Lose a Deathroll";
        override public string Description { get; } = "Triggers whenever you lose a Deathroll (/random to 1)";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        public LoseDeathroll(Plugin plugin) : base(plugin) { }
    }
}
