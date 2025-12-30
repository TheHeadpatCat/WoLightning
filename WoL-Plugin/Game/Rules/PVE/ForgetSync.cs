using Dalamud.Game.Text.SeStringHandling;
using System;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.PVE
{
    // Todo: implement
    public class ForgetSync : RuleBase
    {
        public override string Name { get; } = "Forget to Level Synch";

        public override string Description { get; } = "Triggers whenever you try to attack a Fate Enemy, without synching first.";

        public override RuleCategory Category { get; } = RuleCategory.PVE;

        public ForgetSync() { }

        public ForgetSync(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ToastGui.ErrorToast += Check;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ToastGui.ErrorToast -= Check;
        }

        private void Check(ref SeString messageE, ref bool isHandled)
        {
            try
            {
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.FateLevelNotSynchedTrigger())) Trigger("You failed to Synch your Level!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }

}
