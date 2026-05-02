using Dalamud.Game.Gui.Toast;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Text;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    public class DutyPops : RuleBase
    {
        public override string Name { get; } = "Dutyfinder Pops";

        public override string Description { get; } = "Triggers whenever the 45 second timer for accepting a Duty starts.";

        public override RuleCategory Category { get; } = RuleCategory.Misc;


        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ClientState.CfPop += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ClientState.CfPop -= Check;
        }

        private void Check(ContentFinderCondition condition)
        {
            try
            {
                Trigger("Your Dutyfinder popped!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }
}
