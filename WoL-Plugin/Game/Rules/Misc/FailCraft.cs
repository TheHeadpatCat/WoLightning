using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailCraft : RuleBase
    {
        override public string Name { get; } = "Fail a Craft";
        override public string Description { get; } = "Triggers whenever you fail a Crafting Recipe.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FailCraft() { }
        public FailCraft(Plugin plugin) : base(plugin)
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
                if (Player == null) { Player = Service.ObjectTable.LocalPlayer; return; }
                if (Player.MaxCp == 0) return; // We are not a Crafter.
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.FailCraftTrigger())) Trigger("You have failed a Craft!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if(e.StackTrace != null) Logger.Error(e.StackTrace); }

        }
    }
}
