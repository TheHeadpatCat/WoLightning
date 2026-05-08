using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class SellMarketItem : RuleBase
    {

        public override string Name { get; } = "Sell Market Item";

        public override string Description { get; } = "Triggers whenever the a item that you put on the Market is sold.";

        public override RuleCategory Category { get; } = RuleCategory.Misc;

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ToastGui.QuestToast += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ToastGui.QuestToast -= Check;
        }

        private void Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)
        {
            try
            {
                Logger.Log(4, messageE.ToString());

                if (messageE == null || messageE.ToString() == null) { return; }
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.SellMarketItemTrigger())) Trigger("One of your Items sold on the Market!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }
}
