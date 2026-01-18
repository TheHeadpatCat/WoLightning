using System;
using System.Collections.Generic;
using System.Text;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    public class FailDash : RuleBase
    {
        public override string Name { get; } = "Fail a Dash";
        public override string Description { get; } = "Triggers whenever you use a Dash and die within 3 seconds of it.";
        public override string Hint { get; } = "";
        public override RuleCategory Category { get; } = RuleCategory.PVE;


    }
}
