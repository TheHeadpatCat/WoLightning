using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using WoLightning.Util.Types;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailTreasureInstance : RuleBase
    {
        public override string Name { get; } = "Expelled from a Treasure Dungeon";

        public override string Description { get; } = "Triggers whenever you are Expelled from a Treasure Dungeon, either via trap or wipe.";

        public override RuleCategory Category { get; } = RuleCategory.Misc;

        [JsonIgnore] private static uint[] treasureDungeons = {
            315, // Aquapolis
            408, // Lost Canals
            409, // Hidden Canals
            499, // Shifting Altars
            563, // Dungeon Lyhe
            640, // Shifting Lyhe
            729, // Excitatron
            818, // Shifting Gymnasion
            896, // Cenote Ja Ja
            1059 // Vault
        };

        [JsonConstructor]
        public FailTreasureInstance() { }
        public FailTreasureInstance(Plugin plugin) : base(plugin)
        {
        }

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
            Service.ToastGui.QuestToast += Check;
        }

        private void Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)
        {
            try
            {
                if (messageE == null || messageE.ToString() == null) { return; }
                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.TreasureExpelledTrap()) // terrible
                    || message.Contains(LanguageStrings.TreasureExpelledDevour())
                    || message.Contains(LanguageStrings.TreasureExpelledWake())
                    || message.Contains(LanguageStrings.TreasureExpelledWakeExtra())
                    ) 
                    Trigger("You were expelled from a Treasure Dungeon!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); if (e.StackTrace != null) Logger.Error(e.StackTrace); }
        }
    }
}
