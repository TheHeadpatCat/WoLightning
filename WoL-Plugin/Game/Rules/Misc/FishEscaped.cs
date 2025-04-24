using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FishEscaped : BaseRule
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
            Plugin.ToastGui.QuestToast += Check;
            Player = Plugin.ClientState.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Plugin.ToastGui.QuestToast -= Check;
        }

        private void Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)
        {
            if (Player == null) { Player = Plugin.ClientState.LocalPlayer; return; }
            if (Player.MaxGp == 0) return; // We are not a Gatherer.
            String message = messageE.ToString();
            if (message.Contains(Plugin.LanguageStrings.FishEscapedTrigger())) Trigger("You failed to catch a Fish!");
        }
    }
}
