using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FishEscaped : RuleBase
    {
        override public string Name { get; } = "Fail to catch a Fish";
        override public string Description { get; } = "Triggers whenever a Fish escapes your Rod.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FishEscaped() { }
        public FishEscaped(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ToastGui.QuestToast += Check;
            Player = Service.ObjectTable.LocalPlayer;
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
                Player = Service.ObjectTable.LocalPlayer;
                if (Player == null) { return; }
                if (Player.MaxGp == 0) return; // We are not a Gatherer.
                if (messageE == null || messageE.ToString() == null) { return; }
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.FishEscapedTrigger())) Trigger("You failed to catch a Fish!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }
}
