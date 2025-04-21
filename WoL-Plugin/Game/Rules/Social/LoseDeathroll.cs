using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.Social
{
    public class LoseDeathroll : BaseRule
    {
        override public string Name { get; } = "Lose a Deathroll";
        override public string Description { get; } = "Triggers whenever you lose a Deathroll";
        override public string Hint { get; } = "A Deathroll is when two players do /random on each others numbers until someone reaches 1 and loses.";
        override public RuleCategory Category { get; } = RuleCategory.Social;

        public List<ushort> TriggeringEmotes { get; set; } = new List<ushort>();

        [JsonConstructor]
        public LoseDeathroll() { }
        public LoseDeathroll(Plugin plugin) : base(plugin) { }
    }
}
