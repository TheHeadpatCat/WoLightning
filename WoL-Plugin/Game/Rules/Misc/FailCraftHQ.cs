using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System;
using System.Text.Json.Serialization;
using WoLightning.WoL_Plugin.Util;

namespace WoLightning.WoL_Plugin.Game.Rules.Misc
{
    [Serializable]
    public class FailCraftHQ : RuleBase
    {
        override public string Name { get; } = "Fail a HQ Craft";
        override public string Description { get; } = "Triggers whenever you fail to craft a HQ Item when it would have been possible.";
        override public RuleCategory Category { get; } = RuleCategory.Misc;
        [JsonIgnore] Recipe? CurrentCraft { get; set; }
        [JsonIgnore] IPlayerCharacter Player;


        [JsonConstructor]
        public FailCraftHQ() { }
        public FailCraftHQ(Plugin plugin) : base(plugin)
        {
        }

        override public void Start()
        {
            if (IsRunning) return;
            IsRunning = true;
            Service.ToastGui.QuestToast += Check;
            Player = Service.ClientState.LocalPlayer;
        }

        override public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Service.ToastGui.QuestToast -= Check;
        }

        private unsafe void Check(ref SeString messageE, ref QuestToastOptions options, ref bool isHandled)
        {
            try
            {
                if (Player == null) { Player = Service.ClientState.LocalPlayer; return; }
                if (Player.MaxCp == 0) return; // We are not a Crafter.

                var agent = AgentRecipeNote.Instance();
                CurrentCraft = Service.DataManager.GetExcelSheet<Recipe>().GetRowOrDefault(agent->ActiveCraftRecipeId);
                if (CurrentCraft == null) return;

                String message = messageE.ToString();
                if (message.Contains(LanguageStrings.FailCraftHQTrigger()) && !message.Contains(LanguageStrings.HQSymbol)) Trigger("You have failed a HQ Craft!");
            }
            catch (Exception e) { Logger.Error(Name + " Check() failed."); Logger.Error(e.Message); }
        }
    }
}
