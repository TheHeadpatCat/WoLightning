using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailMeld : RuleBase
    {
        override public string Name { get; } = "Fail to meld Materia";
        override public string Description { get; } = "Triggers whenever you meld Materia and fail.";
        override public string Hint { get; } = "This only triggers when you actually fully fail.\nIf you lose 50 Materia from bulk melding, but still succeed, it won't trigger.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FailMeld() { }
        public FailMeld(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ChatGui.ChatMessage += Check;
            Player = Service.ObjectTable.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ChatGui.ChatMessage -= Check;
        }

        private void Check(XivChatType type, int timestamp, ref SeString senderE, ref SeString messageE, ref bool isHandled)
        {
            try
            {
                Player = Service.ObjectTable.LocalPlayer;
                if (Player == null) return;
                if (messageE == null || messageE.ToString() == null) return;
                Logger.Log(4, $"{type} {senderE} {senderE.TextValue} {messageE.TextValue}");
                if (senderE.TextValue != "" || (int)type != 2114) return;
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.MateriaMeldFailedTrigger())) Trigger("You failed to meld Materia!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }
}
